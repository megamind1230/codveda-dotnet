using FileOrganizer.Core.Models;
using Serilog;

namespace FileOrganizer.Core.Services;

public class FileMover
{
    private readonly ILogger _logger;

    public FileMover(ILogger logger)
    {
        _logger = logger.ForContext<FileMover>();
    }

    public (int totalFiles, long totalSize) Move(List<FileEntry> entries, string sourceDir, bool dryRun, IProgress<double>? progress = null)
    {
        var grouped = entries
            .GroupBy(e => e.TargetFolder)
            .ToDictionary(g => g.Key, g => g.ToList());

        int totalMoved = 0;
        long totalSize = 0;
        int processed = 0;

        foreach (var kvp in grouped)
        {
            var folderName = kvp.Key;
            var folderEntries = kvp.Value;
            var targetDir = Path.Combine(sourceDir, folderName);

            if (!dryRun)
                Directory.CreateDirectory(targetDir);

            foreach (var entry in folderEntries)
            {
                var destFileName = GetUniqueFilePath(targetDir, entry.FileName);
                var destPath = Path.Combine(targetDir, destFileName);

                try
                {
                    if (dryRun)
                    {
                        _logger.Information("[DRY-RUN] {File} -> {Target}/{File}", entry.FileName, folderName, entry.FileName);
                    }
                    else
                    {
                        File.Move(entry.SourcePath, destPath);
                        _logger.Information("[MOVED] {File} -> {Target}/{File}", entry.FileName, folderName, entry.FileName);
                    }

                    totalMoved++;
                    totalSize += entry.SizeBytes;
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "[SKIP] {File}", entry.FileName);
                }

                processed++;
                progress?.Report((double)processed / entries.Count * 100);
            }
        }

        return (totalMoved, totalSize);
    }

    public (int totalFiles, long totalSize) Unorganize(string sourceDir, bool dryRun, IProgress<double>? progress = null)
    {
        var subdirs = Directory.GetDirectories(sourceDir);
        int totalMoved = 0;
        long totalSize = 0;
        int totalFiles = 0;

        foreach (var subdir in subdirs)
        {
            var files = Directory.GetFiles(subdir);
            foreach (var filePath in files)
            {
                totalFiles++;
            }
        }

        if (totalFiles == 0)
        {
            _logger.Information("[INFO] No files found in subdirectories.");
            return (0, 0);
        }

        int processed = 0;

        foreach (var subdir in subdirs)
        {
            var files = Directory.GetFiles(subdir);
            foreach (var filePath in files)
            {
                var fileName = Path.GetFileName(filePath);
                var destFileName = GetUniqueFilePath(sourceDir, fileName);
                var destPath = Path.Combine(sourceDir, destFileName);

                try
                {
                    var size = new FileInfo(filePath).Length;

                    if (dryRun)
                    {
                        _logger.Information("[DRY-RUN] {File} <- {Target}/{File}", fileName, Path.GetFileName(subdir), fileName);
                    }
                    else
                    {
                        File.Move(filePath, destPath);
                        _logger.Information("[MOVED] {File} <- {Target}/{File}", fileName, Path.GetFileName(subdir), fileName);
                    }

                    totalMoved++;
                    totalSize += size;
                }
                catch (Exception ex)
                {
                    _logger.Warning(ex, "[SKIP] {File}", fileName);
                }

                processed++;
                progress?.Report((double)processed / totalFiles * 100);
            }
        }

        return (totalMoved, totalSize);
    }

    private static string GetUniqueFilePath(string targetDir, string fileName)
    {
        var destPath = Path.Combine(targetDir, fileName);
        if (!File.Exists(destPath))
            return fileName;

        var nameWithoutExt = Path.GetFileNameWithoutExtension(fileName);
        var ext = Path.GetExtension(fileName);
        int counter = 1;

        do
        {
            var uniqueName = $"{nameWithoutExt}({counter}){ext}";
            destPath = Path.Combine(targetDir, uniqueName);
            counter++;
        } while (File.Exists(destPath));

        return Path.GetFileName(destPath);
    }
}
