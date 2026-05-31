namespace FileOrganizer.Core.Models;

public class OrganizerConfig
{
    public string SourceDirectory { get; init; } = string.Empty;
    public List<string> NamedPatterns { get; init; } = new();
    public bool DryRun { get; init; }
    public bool CaseSensitive { get; init; } = false;
    public bool Reverse { get; init; }
    public bool Interactive { get; init; }
    public bool UseRegex { get; init; }
}
