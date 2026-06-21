using System.Collections.Concurrent;
using System.Text.RegularExpressions;
using ncdu_clone_dotnet.Models;

namespace ncdu_clone_dotnet.Services;

public class DirectoryScanner
{
    private readonly LogService _log;
    private readonly int _topN;
    private readonly string? _excludePattern;
    private readonly long _minSize;
    private readonly Regex? _excludeRegex;

    // thread-safe collection for tracking largest files across parallel workers
    private readonly ConcurrentBag<FileEntry> _largestFiles = [];

    public DirectoryScanner(LogService log, int topN = 10, string? excludePattern = null, long minSize = 0)
    {
        _log = log;
        _topN = topN;
        _excludePattern = excludePattern;
        _minSize = minSize;
        _excludeRegex = excludePattern is not null ? WildcardToRegex(excludePattern) : null;
    }

    public async Task<FileEntry> ScanAsync(
        string rootPath,
        int maxDepth,
        IProgress<ScanProgress>? progress,
        CancellationToken ct)
    {
        _largestFiles.Clear();
        var filterDesc = BuildFilterDescription();
        _log.Info($"Starting scan of {rootPath} (max depth: {maxDepth}{filterDesc})");

        var root = await ScanDirectoryAsync(rootPath, 0, maxDepth, progress, ct);

        _log.Info($"Scan complete: {root.DirCount} dirs, {root.FileCount} files, {root.Size} bytes");
        return root;
    }

    // #baka: recursion for subdirs, but bounded by maxDepth so we won't stack-overflow
    private async Task<FileEntry> ScanDirectoryAsync(
        string dirPath,
        int depth,
        int maxDepth,
        IProgress<ScanProgress>? progress,
        CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();

        var entry = new FileEntry
        {
            Name = Path.GetFileName(dirPath) + "/",
            FullPath = dirPath,
            IsDirectory = true,
        };

        string[]? subdirs = null;
        string[]? files = null;

        // #baka: Directory I/O is synchronous .NET API, so we offload it to the ThreadPool
        //        via Task.Run to keep the method truly async and not block the caller.
        try
        {
            subdirs = await Task.Run(() => Directory.GetDirectories(dirPath), ct);
            files = await Task.Run(() => Directory.GetFiles(dirPath), ct);
        }
        catch (UnauthorizedAccessException ex)
        {
            entry.ErrorCount++;
            _log.Error($"[{dirPath}] Access denied: {ex.Message}");
            return entry;
        }
        catch (DirectoryNotFoundException)
        {
            entry.ErrorCount++;
            _log.Error($"[{dirPath}] Directory not found");
            return entry;
        }
        catch (OperationCanceledException)
        {
            throw;
        }

        // Filter out excluded subdirectories
        subdirs = subdirs.Where(s => !IsExcluded(Path.GetFileName(s), true)).ToArray();

        // Filter out excluded files
        files = files.Where(f => !IsExcluded(Path.GetFileName(f), false)).ToArray();

        long totalSize = 0;
        int localErrors = 0;
        int fileCount = 0;
        Parallel.ForEach(files, file =>
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                var fi = new FileInfo(file);
                var len = fi.Exists ? fi.Length : 0;

                if (len >= _minSize)
                {
                    Interlocked.Add(ref totalSize, len);
                    Interlocked.Increment(ref fileCount);

                    var fe = new FileEntry
                    {
                        Name = fi.Name,
                        FullPath = fi.FullName,
                        Size = len,
                        IsDirectory = false,
                    };
                    // #baka: ConcurrentBag is already thread-safe, lock is redundant here
                    //        but kept to demonstrate explicit synchronization.
                    lock (_largestFiles)
                    {
                        _largestFiles.Add(fe);
                    }
                }
            }
            catch (UnauthorizedAccessException)
            {
                Interlocked.Increment(ref localErrors);
            }
        });
        entry.FileCount = fileCount;
        entry.ErrorCount += localErrors;

        entry.Size = totalSize;

        // Recurse into subdirectories (skip if max depth reached)
        if (depth < maxDepth)
        {
            var subdirTasks = new List<Task<FileEntry>>();
            foreach (var subdir in subdirs)
            {
                // #baka: We fan-out one Task per subdirectory so they run concurrently.
                //        Task.WhenAll awaits all of them at once.
                subdirTasks.Add(ScanDirectoryAsync(subdir, depth + 1, maxDepth, progress, ct));
            }

            var results = await Task.WhenAll(subdirTasks);

            foreach (var child in results)
            {
                entry.Children.Add(child);
                entry.Size += child.Size;
                entry.FileCount += child.FileCount;
                entry.DirCount += child.DirCount + 1;
                entry.ErrorCount += child.ErrorCount;
            }
        }

        progress?.Report(new ScanProgress
        {
            CurrentPath = dirPath,
            BytesFound = entry.Size,
            DirectoriesScanned = entry.DirCount,
            FilesScanned = entry.FileCount,
            Errors = entry.ErrorCount,
        });

        return entry;
    }

    public List<FileEntry> GetTopFiles(int n)
    {
        // #baka: sort all collected files by size descending, take top N
        return _largestFiles.OrderByDescending(f => f.Size).Take(n).ToList();
    }

    private bool IsExcluded(string name, bool isDir)
    {
        if (_excludeRegex is null)
            return false;
        return _excludeRegex.IsMatch(name);
    }

    // #baka: converts user glob (e.g. "*.tmp", "node_modules") to a regex.
    //        Escape everything first, then un-escape * → .* and ? → . for wildcard matching.
    private static Regex WildcardToRegex(string pattern)
    {
        var escaped = Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".");
        return new Regex($"^{escaped}$", RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }

    private string BuildFilterDescription()
    {
        var parts = new List<string>();
        if (_excludePattern is not null)
            parts.Add($"exclude={_excludePattern}");
        if (_minSize > 0)
            parts.Add($"min-size={_minSize}");
        return parts.Count > 0 ? ", " + string.Join(", ", parts) : "";
    }
}
