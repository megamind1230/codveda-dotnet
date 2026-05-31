using FileOrganizer.Core.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var config = ParseArgs(args);
if (config == null)
    return;

var organizer = new FileOrganizer.Core.FileOrganizer();
organizer.Run(config);

Log.CloseAndFlush();

static OrganizerConfig? ParseArgs(string[] args)
{
    string? sourceDir = null;
    var namedPatterns = new List<string>();
    bool dryRun = false;
    bool reverse = false;
    bool oneShot = false;
    bool useRegex = false;

    for (int i = 0; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--source":
                if (i + 1 < args.Length)
                    sourceDir = args[++i];
                break;
            case "--patterns":
                if (i + 1 < args.Length)
                {
                    namedPatterns = args[++i]
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .ToList();
                }
                break;
            case "--dry-run":
                dryRun = true;
                break;
            case "--reverse":
                reverse = true;
                break;
            case "--one-shot":
                oneShot = true;
                break;
            case "--regex":
                useRegex = true;
                break;
        }
    }

    if (string.IsNullOrEmpty(sourceDir))
    {
        Console.WriteLine("[ERROR] --source is required.");
        Console.WriteLine("Usage: --source <path> [--patterns \"kw1,kw2\"] [--dry-run] [--reverse] [--one-shot] [--regex]");
        return null;
    }

    if (!Directory.Exists(sourceDir))
    {
        Console.WriteLine($"[ERROR] Source directory does not exist: {sourceDir}");
        return null;
    }

    return new OrganizerConfig
    {
        SourceDirectory = sourceDir,
        NamedPatterns = namedPatterns,
        DryRun = dryRun,
        Reverse = reverse,
        Interactive = !oneShot,
        UseRegex = useRegex
    };
}
