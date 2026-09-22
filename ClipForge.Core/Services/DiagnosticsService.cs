using System.Text;

namespace ClipForge.Core.Services;

public class DiagnosticsService
{
    private static readonly object _lock = new();
    private readonly string _logFile;
    private bool _loggingEnabled = true;

    public DiagnosticsService(string? logDirectory = null)
    {
        var logDir = logDirectory ?? StorageService.GetDefaultLogsDirectory();
        try
        {
            if (!Directory.Exists(logDir))
                Directory.CreateDirectory(logDir);
        }
        catch { }

        _logFile = Path.Combine(logDir, "clipforge.log");
    }

    public void SetLoggingEnabled(bool enabled) => _loggingEnabled = enabled;
    public void LogInfo(string message) => WriteLog("INFO", message);
    public void LogWarning(string message) => WriteLog("WARN", message);

    public void LogError(string message, Exception? ex = null)
    {
        var sb = new StringBuilder(message);
        if (ex != null)
        {
            sb.AppendLine();
            sb.Append($"{ex.GetType().Name}: {ex.Message}");
            sb.AppendLine();
            sb.Append(ex.StackTrace);
        }
        WriteLog("ERROR", sb.ToString());
    }

    private void WriteLog(string level, string message)
    {
        if (!_loggingEnabled) return;
        try
        {
            lock (_lock)
            {
                var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";
                File.AppendAllText(_logFile, line, Encoding.UTF8);
            }
        }
        catch { }
    }

    public string GetDiagnosticsReport()
    {
        var sb = new StringBuilder();
        sb.AppendLine("=== ClipForge Diagnostics Report ===");
        sb.AppendLine($"Timestamp: {DateTime.Now:u}");
        sb.AppendLine($"OS: {Environment.OSVersion}");
        sb.AppendLine($"64-Bit OS: {Environment.Is64BitOperatingSystem}");
        sb.AppendLine($"Process Architecture: {System.Runtime.InteropServices.RuntimeInformation.ProcessArchitecture}");
        sb.AppendLine($"Processor Count: {Environment.ProcessorCount}");
        sb.AppendLine($".NET Runtime: {Environment.Version}");
        sb.AppendLine($"Working Set Memory: {Environment.WorkingSet / (1024 * 1024)} MB");
        return sb.ToString();
    }
}
