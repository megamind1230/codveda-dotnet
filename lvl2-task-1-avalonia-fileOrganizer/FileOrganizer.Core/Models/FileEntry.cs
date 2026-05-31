namespace FileOrganizer.Core.Models;

public class FileEntry
{
    public string SourcePath { get; init; } = string.Empty;
    public string FileName { get; init; } = string.Empty;
    public string Extension { get; init; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string TargetFolder { get; set; } = string.Empty;
    public long SizeBytes { get; init; }
    public bool IsMatchedByPattern { get; set; }

    public override bool Equals(object? obj) =>
        obj is FileEntry other && SourcePath == other.SourcePath;

    public override int GetHashCode() =>
        SourcePath.GetHashCode();
}
