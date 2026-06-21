using System.Text;

namespace ncdu_clone_dotnet.Models;

public class FileEntry
{
    public string Name { get; init; } = "";
    public string FullPath { get; init; } = "";
    public long Size { get; set; }
    public bool IsDirectory { get; init; }
    public List<FileEntry> Children { get; } = [];
    public int ErrorCount { get; set; }
    public int FileCount { get; set; }
    public int DirCount { get; set; }

    public string PrettyPrint(string indent = "", bool isLast = true, bool human = true)
    {
        var sb = new StringBuilder();
        var prefix = indent + (isLast ? "└── " : "├── ");
        var sizeStr = human ? HumanSize(Size) : $"{Size:N0} B";

        if (IsDirectory)
            sb.AppendLine($"{prefix}{Name}/  ({sizeStr})");
        else
            sb.AppendLine($"{prefix}{Name}  ({sizeStr})");

        var childIndent = indent + (isLast ? "    " : "│   ");
        for (int i = 0; i < Children.Count; i++)
            sb.Append(Children[i].PrettyPrint(childIndent, i == Children.Count - 1, human));

        return sb.ToString();
    }

    public static string HumanSize(long bytes)
    {
        return bytes switch
        {
            < 1024 => $"{bytes} B",
            < 1024L * 1024 => $"{bytes / 1024.0:F1} KB",
            < 1024L * 1024 * 1024 => $"{bytes / (1024.0 * 1024):F1} MB",
            _ => $"{bytes / (1024.0 * 1024 * 1024):F2} GB"
        };
    }
}
