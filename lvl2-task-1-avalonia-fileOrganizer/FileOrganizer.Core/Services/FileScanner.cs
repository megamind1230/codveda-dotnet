using FileOrganizer.Core.Models;
using Serilog;

namespace FileOrganizer.Core.Services;

public class FileScanner
{
    private readonly ILogger _logger;

    public FileScanner(ILogger logger)
    {
        _logger = logger.ForContext<FileScanner>();
    }

    public List<FileEntry> Scan(string directoryPath)
    {
        var entries = new List<FileEntry>();

        foreach (var filePath in Directory.EnumerateFiles(directoryPath))
        {
            try
            {
                var fileInfo = new FileInfo(filePath);
                entries.Add(new FileEntry
                {
                    SourcePath = filePath,
                    FileName = fileInfo.Name,
                    Extension = fileInfo.Extension,
                    SizeBytes = fileInfo.Length
                });
            }
            catch (Exception ex) when (ex is UnauthorizedAccessException or PathTooLongException)
            {
                _logger.Warning(ex, "Skipping inaccessible file: {FilePath}", filePath);
            }
        }

        _logger.Information("Scanned {Count} files from {Directory}", entries.Count, directoryPath);
        return entries;
    }
}
