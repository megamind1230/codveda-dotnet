using System.Diagnostics;

namespace ncdu_clone_dotnet.Services;

public class LogService : IDisposable
{
    private readonly string _logDir;
    private readonly StreamWriter _writer;
    private readonly object _lock = new();

    private static readonly string LogRoot =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "magnus", "ncdu-clone-dotnet", "logs");

    public LogService()
    {
        _logDir = LogRoot;
        Directory.CreateDirectory(_logDir);
        CleanupOldLogs();

        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var logPath = Path.Combine(_logDir, $"scan-{timestamp}.log");
        _writer = new StreamWriter(logPath, append: true) { AutoFlush = true };
    }

    public void Info(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] INFO  {message}";
        lock (_lock)
        {
            _writer.WriteLine(line);
        }
    }

    public void Error(string message)
    {
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] ERROR {message}";
        lock (_lock)
        {
            _writer.WriteLine(line);
        }
        Console.Error.WriteLine($"ERROR: {message}");
    }

    private void CleanupOldLogs()
    {
        var files = Directory.GetFiles(_logDir, "scan-*.log")
                             .OrderByDescending(f => f)
                             .Skip(4)
                             .ToList();
        foreach (var f in files)
            try { File.Delete(f); } catch { /* best effort */ }
    }

    public void Dispose()
    {
        _writer?.Dispose();
    }
}
