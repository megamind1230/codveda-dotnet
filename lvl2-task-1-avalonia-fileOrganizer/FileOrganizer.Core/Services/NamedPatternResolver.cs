using System.Text.RegularExpressions;
using FileOrganizer.Core.Models;
using Serilog;

namespace FileOrganizer.Core.Services;

public class NamedPatternResolver
{
    private readonly ILogger _logger;

    public NamedPatternResolver(ILogger logger)
    {
        _logger = logger.ForContext<NamedPatternResolver>();
    }

    public List<FileEntry> Match(IEnumerable<FileEntry> files, List<string> patterns, bool caseSensitive, bool useRegex = false)
    {
        var matched = new List<FileEntry>();
        var sanitized = patterns
            .Select(p => p.Trim())
            .Where(p => !string.IsNullOrEmpty(p))
            .ToList();

        if (sanitized.Count == 0)
            return matched;

        _logger.Information("Matching {PatternCount} patterns against files (useRegex: {useRegex})", sanitized.Count, useRegex);

        if (useRegex)
        {
            var options = caseSensitive ? RegexOptions.None : RegexOptions.IgnoreCase;

            foreach (var file in files)
            {
                foreach (var pattern in sanitized)
                {
                    try
                    {
                        if (Regex.IsMatch(file.FileName, pattern, options))
                        {
                            file.IsMatchedByPattern = true;
                            file.TargetFolder = SanitizeFolderName(pattern);
                            matched.Add(file);
                            _logger.Debug("Matched {File} with regex pattern {Pattern}", file.FileName, pattern);
                            break;
                        }
                    }
                    catch (ArgumentException ex)
                    {
                        _logger.Warning(ex, "Invalid regex pattern skipped: {Pattern}", pattern);
                        continue;
                    }
                }
            }
        }
        else
        {
            var comparison = caseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;

            foreach (var file in files)
            {
                foreach (var pattern in sanitized)
                {
                    if (file.FileName.Contains(pattern, comparison))
                    {
                        file.IsMatchedByPattern = true;
                        file.TargetFolder = SanitizeFolderName(pattern);
                        matched.Add(file);
                        _logger.Debug("Matched {File} with keyword pattern {Pattern}", file.FileName, pattern);
                        break;
                    }
                }
            }
        }

        _logger.Information("Matched {Count} files by pattern", matched.Count);
        return matched;
    }

    private static string SanitizeFolderName(string name)
    {
        foreach (var c in Path.GetInvalidFileNameChars())
        {
            name = name.Replace(c, '_');
        }
        return name;
    }
}
