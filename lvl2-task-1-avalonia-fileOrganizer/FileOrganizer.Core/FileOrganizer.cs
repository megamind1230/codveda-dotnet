using FileOrganizer.Core.Models;
using FileOrganizer.Core.Services;
using Serilog;

namespace FileOrganizer.Core;

public class FileOrganizer
{
    private readonly ILogger _logger;
    private readonly FileScanner _scanner;
    private readonly NamedPatternResolver _patternResolver;
    private readonly FileMover _mover;

    public FileOrganizer()
    {
        _logger = Log.ForContext<FileOrganizer>();
        _scanner = new FileScanner(Log.ForContext<FileScanner>());
        _patternResolver = new NamedPatternResolver(Log.ForContext<NamedPatternResolver>());
        _mover = new FileMover(Log.ForContext<FileMover>());
    }

    public void Run(OrganizerConfig config)
    {
        if (!Directory.Exists(config.SourceDirectory))
        {
            Console.WriteLine($"[ERROR] Source directory does not exist: {config.SourceDirectory}");
            _logger.Error("Source directory does not exist: {Directory}", config.SourceDirectory);
            return;
        }

        if (config.Interactive)
        {
            RunInteractive(config.SourceDirectory, config.DryRun, config.UseRegex);
            return;
        }

        if (config.Reverse)
        {
            RunReverse(config);
            return;
        }

        _logger.Information("Starting one-shot organization in {Directory}", config.SourceDirectory);
        var allEntries = _scanner.Scan(config.SourceDirectory);

        if (allEntries.Count == 0)
        {
            Console.WriteLine("[INFO] No files found in source directory.");
            return;
        }

        var matchedEntries = new List<FileEntry>();
        var remainingEntries = allEntries;

        if (config.NamedPatterns.Count > 0)
        {
            matchedEntries = _patternResolver.Match(allEntries, config.NamedPatterns, config.CaseSensitive, config.UseRegex);
            remainingEntries = allEntries.Except(matchedEntries).ToList();
        }

        foreach (var entry in remainingEntries)
        {
            var category = CategoryResolver.Resolve(entry.Extension);
            entry.Category = category;
            entry.TargetFolder = category;
        }

        var all = new List<FileEntry>();
        all.AddRange(matchedEntries);
        all.AddRange(remainingEntries);

        var (totalFiles, totalSize) = _mover.Move(all, config.SourceDirectory, config.DryRun);

        var namedCount = matchedEntries.Count;
        var categorizedCount = remainingEntries.Count;
        var sizeMb = totalSize / (1024.0 * 1024.0);

        Console.WriteLine($"[SUMMARY] {totalFiles} files moved ({namedCount} named, {categorizedCount} categorized) — {sizeMb:F1} MB");
        _logger.Information("Summary: {Total} files moved ({Named} named, {Categorized} categorized) — {Size:F1} MB",
            totalFiles, namedCount, categorizedCount, sizeMb);
    }

    private void RunReverse(OrganizerConfig config)
    {
        _logger.Information("Starting reverse mode in {Directory}", config.SourceDirectory);
        Console.WriteLine("[INFO] Reverse mode: moving files from subfolders back to source directory.");
        var (totalFiles, totalSize) = _mover.Unorganize(config.SourceDirectory, config.DryRun);
        var sizeMb = totalSize / (1024.0 * 1024.0);
        Console.WriteLine($"[SUMMARY] {totalFiles} files unorganized — {sizeMb:F1} MB");
        _logger.Information("Reverse summary: {Total} files unorganized — {Size:F1} MB", totalFiles, sizeMb);
    }

    private void RunInteractive(string sourceDir, bool dryRun, bool useRegex)
    {
        var remaining = _scanner.Scan(sourceDir);
        Console.WriteLine($"\n[INFO] Source: {sourceDir}  ({remaining.Count} files found)");
        _logger.Information("Interactive mode started for {Directory} with {Count} files", sourceDir, remaining.Count);

        while (true)
        {
            Console.WriteLine();
            Console.WriteLine("  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine($"     File Organizer  |  {remaining.Count} files remaining");
            Console.WriteLine("  ━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━");
            Console.WriteLine("    1. Container   2. Patterns");
            Console.WriteLine("    3. General     l. List");
            Console.WriteLine("    r. Reverse     q. Quit");
            Console.Write("  Select: ");

            var choice = (Console.ReadLine() ?? "").Trim().ToLower();

            switch (choice)
            {
                case "1":
                    ContainerPhase(ref remaining, sourceDir, dryRun);
                    break;
                case "2":
                    PatternsPhase(ref remaining, sourceDir, dryRun, useRegex);
                    break;
                case "3":
                    GeneralPhase(ref remaining, sourceDir, dryRun);
                    break;
                case "l":
                    Console.WriteLine("\n  Remaining files:");
                    if (remaining.Count == 0)
                        Console.WriteLine("    (none)");
                    else
                        for (int i = 0; i < remaining.Count; i++)
                            Console.WriteLine($"    {i + 1}. {remaining[i].FileName}");
                    break;
                case "r":
                    ReversePhase(sourceDir, dryRun);
                    remaining = _scanner.Scan(sourceDir);
                    break;
                case "q":
                    Console.WriteLine("[INFO] Exiting.");
                    _logger.Information("Interactive mode exited by user");
                    return;
                default:
                    Console.WriteLine("[ERROR] Invalid choice.");
                    break;
            }

            if (remaining.Count == 0 && choice != "r" && choice != "q" && choice != "l")
            {
                Console.WriteLine("[INFO] All files organized!");
                Console.WriteLine("[INFO] Type 'q' to quit or 'r' to reverse.");
            }
        }
    }

    private void ContainerPhase(ref List<FileEntry> remaining, string sourceDir, bool dryRun)
    {
        if (remaining.Count == 0)
        {
            Console.WriteLine("[INFO] No files remaining.");
            return;
        }

        Console.WriteLine("\n  Files:");
        for (int i = 0; i < remaining.Count; i++)
            Console.WriteLine($"    {i + 1}. {remaining[i].FileName}");

        Console.Write("\n  Select files by index (e.g. 2,3 or 1-3): ");
        var input = Console.ReadLine() ?? "";
        var indices = ParseIndices(input, remaining.Count);
        if (indices.Count == 0)
        {
            Console.WriteLine("[ERROR] No valid indices provided.");
            return;
        }

        Console.Write("  Container folder name: ");
        var folderName = (Console.ReadLine() ?? "").Trim();
        if (string.IsNullOrEmpty(folderName))
        {
            Console.WriteLine("[ERROR] Folder name cannot be empty.");
            return;
        }

        var selected = new List<FileEntry>();
        var newRemaining = new List<FileEntry>();

        for (int i = 0; i < remaining.Count; i++)
        {
            if (indices.Contains(i))
            {
                remaining[i].TargetFolder = folderName;
                selected.Add(remaining[i]);
            }
            else
            {
                newRemaining.Add(remaining[i]);
            }
        }

        _logger.Information("Container phase: moving {Count} files to {Folder}", selected.Count, folderName);
        _mover.Move(selected, sourceDir, dryRun);
        remaining = newRemaining;
    }

    private void PatternsPhase(ref List<FileEntry> remaining, string sourceDir, bool dryRun, bool useRegex = false)
    {
        if (remaining.Count == 0)
        {
            Console.WriteLine("[INFO] No files remaining.");
            return;
        }

        Console.Write("\n  Enable Regex? (y/n): ");
        var regexChoice = (Console.ReadLine() ?? "").Trim().ToLower();
        if (regexChoice is "y" or "yes")
            useRegex = true;
        else
            useRegex = false;

        var prompt = useRegex
            ? "\n  Enter comma-separated regex patterns: "
            : "\n  Enter comma-separated keywords: ";
        Console.Write(prompt);
        var input = Console.ReadLine() ?? "";
        var patterns = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

        if (patterns.Count == 0)
        {
            Console.WriteLine("[ERROR] No patterns provided.");
            return;
        }

        var matched = _patternResolver.Match(remaining, patterns, false, useRegex);
        if (matched.Count == 0)
        {
            Console.WriteLine("[INFO] No files matched the patterns.");
            return;
        }

        _logger.Information("Patterns phase: matched and moving {Count} files", matched.Count);
        _mover.Move(matched, sourceDir, dryRun);
        remaining = remaining.Except(matched).ToList();
    }

    private void GeneralPhase(ref List<FileEntry> remaining, string sourceDir, bool dryRun)
    {
        if (remaining.Count == 0)
        {
            Console.WriteLine("[INFO] No files remaining.");
            return;
        }

        foreach (var entry in remaining)
        {
            entry.Category = CategoryResolver.Resolve(entry.Extension);
            entry.TargetFolder = entry.Category;
        }

        _logger.Information("General phase: moving {Count} files by category", remaining.Count);
        _mover.Move(remaining, sourceDir, dryRun);
        remaining.Clear();
    }

    private void ReversePhase(string sourceDir, bool dryRun)
    {
        _logger.Information("Reverse phase triggered");
        _mover.Unorganize(sourceDir, dryRun);
    }

    private static HashSet<int> ParseIndices(string input, int max)
    {
        var result = new HashSet<int>();
        var parts = input.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        foreach (var part in parts)
        {
            var trimmed = part.Trim();
            var rangeParts = trimmed.Split('-', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

            if (rangeParts.Length == 2)
            {
                if (int.TryParse(rangeParts[0], out var start) && int.TryParse(rangeParts[1], out var end))
                {
                    for (int i = start; i <= end; i++)
                    {
                        if (i >= 1 && i <= max)
                            result.Add(i - 1);
                    }
                }
            }
            else if (int.TryParse(trimmed, out var num))
            {
                if (num >= 1 && num <= max)
                    result.Add(num - 1);
            }
        }

        return result;
    }
}
