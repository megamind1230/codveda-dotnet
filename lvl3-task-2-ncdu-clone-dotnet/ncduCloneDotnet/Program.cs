using System.Diagnostics;
using ncdu_clone_dotnet.Models;
using ncdu_clone_dotnet.Services;

var (rootPath, maxDepth, topN, human, excludePattern, minSize) = ParseArgs(args);

using var log = new LogService();
var scanner = new DirectoryScanner(log, topN, excludePattern, minSize);
var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
    log.Info("Cancellation requested by user (Ctrl+C)");
};

var filterInfo = BuildFilterInfo(excludePattern, minSize);
Console.WriteLine($"Scanning: {rootPath}  (max depth: {maxDepth}, top {topN}{filterInfo})\n");
log.Info($"CLI args: path={rootPath}, maxDepth={maxDepth}, topN={topN}, human={human}, exclude={excludePattern ?? "(none)"}, minSize={minSize}");

var progress = new Progress<ScanProgress>(p =>
{
    var bar = RenderBar(p.Percent);
    var line = $"  {TruncatePath(p.CurrentPath, 50),-50}  {bar}  {p.DirectoriesScanned,5} dirs  {p.FilesScanned,7} files  ({FileEntry.HumanSize(p.BytesFound),10})";
    Console.Write("\r" + line.PadRight(Console.WindowWidth - 1));
});

var sw = Stopwatch.StartNew();
FileEntry? root = null;
try
{
    root = await scanner.ScanAsync(rootPath, maxDepth, progress, cts.Token);
}
catch (OperationCanceledException)
{
    Console.WriteLine("\n\nScan cancelled.");
    log.Info("Scan cancelled by user");
    return 1;
}
catch (Exception ex)
{
    Console.WriteLine($"\n\nFatal error: {ex.Message}");
    log.Error($"Fatal: {ex}");
    return 1;
}

sw.Stop();
Console.WriteLine("\n\nResults:");
Console.WriteLine(root.PrettyPrint(human: human));

// top N largest files
var topFiles = scanner.GetTopFiles(topN);
if (topFiles.Count > 0)
{
    Console.WriteLine($"\nTop {topN} largest files:");
    for (int i = 0; i < topFiles.Count; i++)
    {
        var f = topFiles[i];
        var size = human ? FileEntry.HumanSize(f.Size) : $"{f.Size:N0} B";
        Console.WriteLine($"  {i + 1,2}. {size,10}  {f.FullPath}");
    }
}

Console.WriteLine($"\n{root.DirCount + 1} directories, {root.FileCount} files  —  scanned in {sw.Elapsed.TotalSeconds:F2}s  (errors: {root.ErrorCount})");
log.Info($"Completed: {root.DirCount + 1} dirs, {root.FileCount} files, {root.Size} bytes, {sw.Elapsed.TotalSeconds:F2}s");

#if DEBUG
RaceConditionDemo();
#endif

return 0;

// ---- local functions ----

static (string path, int maxDepth, int topN, bool human, string? exclude, long minSize) ParseArgs(string[] args)
{
    var maxDepth = 10;
    var topN = 10;
    var human = true;
    string? path = null;
    string? exclude = null;
    long minSize = 0;

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--source" when i + 1 < args.Length:
                path = args[++i];
                break;
            case "--max-depth" when i + 1 < args.Length:
                maxDepth = int.Parse(args[++i]);
                break;
            case "--top" when i + 1 < args.Length:
                topN = int.Parse(args[++i]);
                break;
            case "--exclude" when i + 1 < args.Length:
                exclude = args[++i];
                break;
            case "--min-size" when i + 1 < args.Length:
                minSize = ParseSize(args[++i]);
                break;
            case "-h":
            case "--human":
                human = true;
                break;
        }
    }

    path = Path.GetFullPath(path ?? ".");
    return (path, maxDepth, topN, human, exclude, minSize);
}

// #baka: parse --min-size with optional K/M/G suffix
static long ParseSize(string value)
{
    value = value.Trim().ToUpperInvariant();
    var multipliers = new Dictionary<string, long>
    {
        ["K"] = 1024,
        ["M"] = 1024 * 1024,
        ["G"] = 1024 * 1024 * 1024,
    };

    foreach (var (suffix, mult) in multipliers)
    {
        if (value.EndsWith(suffix) && long.TryParse(value[..^1], out var num))
            return num * mult;
    }

    if (long.TryParse(value, out var plain))
        return plain;

    return 0;
}

static string BuildFilterInfo(string? exclude, long minSize)
{
    var parts = new List<string>();
    if (exclude is not null)
        parts.Add($"exclude={exclude}");
    if (minSize > 0)
        parts.Add($"min-size={ParseSizePretty(minSize)}");
    return parts.Count > 0 ? ", " + string.Join(", ", parts) : "";
}

static string ParseSizePretty(long bytes)
{
    return bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024L * 1024 => $"{bytes / 1024.0:F0} KB",
        < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
        _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
    };
}

// #baka: simple percentage bar — fills blocks up to the computed fraction
static string RenderBar(double percent)
{
    var barWidth = 20;
    var filled = (int)(percent / 100.0 * barWidth);
    filled = Math.Clamp(filled, 0, barWidth);
    var bar = new string('█', filled) + new string('░', barWidth - filled);
    return $"{bar} {percent,5:F1}%";
}

static string TruncatePath(string path, int maxLen)
{
    return path.Length <= maxLen ? path : "…" + path[^(maxLen - 1)..];
}

// #baka: demonstrates a race condition caused by non-atomic += on a shared variable
//        inside Parallel.ForEach. Run this to see wrong totals, then compare with the
//        Interlocked.Add fix used in DirectoryScanner.
static void RaceConditionDemo()
{
    Console.WriteLine("\n--- Race condition demo (DEBUG) ---");
    var nums = Enumerable.Range(1, 100_000).ToArray();
    long brokenSum = 0;
    Parallel.ForEach(nums, n => { brokenSum += n; });
    long correctSum = (long)nums.Length * (nums.Length + 1) / 2;
    Console.WriteLine($"  Broken (plain +=):    {brokenSum:N0}");
    Console.WriteLine($"  Correct (expected):   {correctSum:N0}");
    Console.WriteLine($"  Difference:           {correctSum - brokenSum:N0} (lost updates)");

    long fixedSum = 0;
    Parallel.ForEach(nums, n => Interlocked.Add(ref fixedSum, n));
    Console.WriteLine($"  Fixed (Interlocked):  {fixedSum:N0}");
}

// #baka: deadlock demo. Calling .Result on a Task inside a Task.Run or sync context
//        can deadlock when the SynchronizationContext is captured and the task needs
//        that same context to resume. This is commented out by default to avoid hanging.
/*
static void DeadlockDemo()
{
    Console.WriteLine("\n--- Deadlock demo (DEBUG, disabled) ---");
    Console.WriteLine("Uncomment to see the app hang. Fix: use await everywhere + ConfigureAwait(false).");
    // var task = Task.Run(() => DemoDeadlockAsync().Result);
    // task.Wait();
}

static async Task DemoDeadlockAsync()
{
    await Task.Delay(100).ConfigureAwait(true);
}
*/
