namespace FileOrganizer.Core.Services;

public static class CategoryResolver
{
    private static readonly Dictionary<string, string> Map = new(StringComparer.OrdinalIgnoreCase)
    {
        { ".jpg", "imgs" },
        { ".jpeg", "imgs" },
        { ".png", "imgs" },
        { ".gif", "imgs" },
        { ".bmp", "imgs" },
        { ".webp", "imgs" },
        { ".svg", "imgs" },
        { ".ico", "imgs" },
        { ".pdf", "docs" },
        { ".doc", "docs" },
        { ".docx", "docs" },
        { ".xls", "docs" },
        { ".xlsx", "docs" },
        { ".ppt", "docs" },
        { ".pptx", "docs" },
        { ".txt", "docs" },
        { ".rtf", "docs" },
        { ".csv", "docs" },
        { ".md", "docs" },
        { ".mp3", "aud" },
        { ".wav", "aud" },
        { ".flac", "aud" },
        { ".aac", "aud" },
        { ".ogg", "aud" },
        { ".wma", "aud" },
        { ".m4a", "aud" },
        { ".mp4", "vid" },
        { ".mkv", "vid" },
        { ".avi", "vid" },
        { ".mov", "vid" },
        { ".wmv", "vid" },
        { ".flv", "vid" },
        { ".webm", "vid" },
        { ".zip", "archives" },
        { ".rar", "archives" },
        { ".7z", "archives" },
        { ".tar", "archives" },
        { ".gz", "archives" },
        { ".bz2", "archives" },
        { ".exe", "installers" },
        { ".msi", "installers" },
        { ".deb", "installers" },
        { ".rpm", "installers" },
        { ".appimage", "installers" },
    };

    public static string Resolve(string extension)
    {
        return Map.TryGetValue(extension, out var category) ? category : "others";
    }
}
