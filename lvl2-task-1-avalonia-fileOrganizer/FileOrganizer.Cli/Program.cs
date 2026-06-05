using FileOrganizer.Core.Models;
using Serilog;

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Debug()
    .WriteTo.Console()
    .WriteTo.File("logs/app-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

ReadLine.AutoCompletionHandler = new FilePathCompletion();

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
        sourceDir = ReadLine.Read("Enter source directory path: ").Trim();
        ExpandTilde(ref sourceDir);
        while (string.IsNullOrEmpty(sourceDir) || !Directory.Exists(sourceDir))
        {
            if (string.IsNullOrEmpty(sourceDir))
                Console.WriteLine("[ERROR] Path cannot be empty.");
            else
                Console.WriteLine($"[ERROR] Directory does not exist: {sourceDir}");
            sourceDir = ReadLine.Read("Enter source directory path: ").Trim();
            ExpandTilde(ref sourceDir);
        }
    }
    else
    {
        ExpandTilde(ref sourceDir);
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

static void ExpandTilde(ref string? path)
{
    if (string.IsNullOrEmpty(path)) return;
    if (path.StartsWith('~'))
    {
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        path = home + path[1..];
    }
}

class FilePathCompletion : IAutoCompleteHandler
{
    public char[] Separators { get; set; } = [];

    public string[] GetSuggestions(string text, int index)
    {
        if (string.IsNullOrEmpty(text))
            return SuggestInDir(Directory.GetCurrentDirectory(), "", "");

        if (text.StartsWith('~'))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            if (text.Length == 1)
                return SuggestInDir(home, "", "~/");

            var afterTilde = text[1..];
            var lookupPath = home + afterTilde;
            var dir = Path.GetDirectoryName(lookupPath) ?? home;
            var prefix = Path.GetFileName(lookupPath);
            var resultPrefix = "~" + afterTilde[..^prefix.Length];
            return SuggestInDir(dir, prefix, resultPrefix);
        }

        var searchDir = Path.GetDirectoryName(text);
        var fileName = Path.GetFileName(text);

        if (string.IsNullOrEmpty(searchDir))
            return SuggestInDir(Directory.GetCurrentDirectory(), fileName, "");

        var resultPre = text[..^fileName.Length];
        return SuggestInDir(searchDir, fileName, resultPre);
    }

    static string[] SuggestInDir(string dir, string prefix, string resultPrefix)
    {
        if (!Directory.Exists(dir))
            return [];

        try
        {
            return Directory.GetFileSystemEntries(dir, prefix + "*")
                .Select(e =>
                {
                    var name = Path.GetFileName(e);
                    var suffix = Directory.Exists(e) ? "/" : "";
                    return resultPrefix + name + suffix;
                })
                .ToArray();
        }
        catch
        {
            return [];
        }
    }
}
