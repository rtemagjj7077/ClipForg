using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ClipForge.Core.Services;

public class StorageService
{
    private readonly DiagnosticsService _diagnostics;

    public string DataDirectory { get; }
    public string ThumbnailsDirectory { get; }
    public string LogsDirectory { get; }
    public string DefaultBufferDirectory { get; }
    public string DefaultClipsDirectory { get; }

    public StorageService(DiagnosticsService diagnostics)
    {
        _diagnostics = diagnostics;
        DataDirectory = GetDefaultDataDirectory();
        ThumbnailsDirectory = Path.Combine(DataDirectory, "thumbnails");
        LogsDirectory = Path.Combine(DataDirectory, "logs");
        DefaultBufferDirectory = Path.Combine(DataDirectory, "buffer");

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            DefaultClipsDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "ClipForge");
        }
        else
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            DefaultClipsDirectory = Path.Combine(home, "Videos", "ClipForge");
        }

        EnsureDirectoriesExist();
    }

    public static string GetDefaultDataDirectory()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClipForge");
        }
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".local", "share", "ClipForge");
    }

    public static string GetDefaultLogsDirectory() => Path.Combine(GetDefaultDataDirectory(), "logs");

    public void EnsureDirectoriesExist(string? customClipsDir = null, string? customBufferDir = null)
    {
        try
        {
            Directory.CreateDirectory(DataDirectory);
            Directory.CreateDirectory(ThumbnailsDirectory);
            Directory.CreateDirectory(LogsDirectory);
            Directory.CreateDirectory(customBufferDir ?? DefaultBufferDirectory);
            Directory.CreateDirectory(customClipsDir ?? DefaultClipsDirectory);
        }
        catch (Exception ex)
        {
            _diagnostics.LogError("Failed to initialize storage directories", ex);
        }
    }

    public async Task<long> GetFolderSizeBytesAsync(string folderPath)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(folderPath)) return 0;
                var di = new DirectoryInfo(folderPath);
                return di.EnumerateFiles("*", SearchOption.AllDirectories).Sum(fi => fi.Length);
            }
            catch (Exception ex)
            {
                _diagnostics.LogError($"Failed to calculate folder size for {folderPath}", ex);
                return 0;
            }
        });
    }

    public async Task CleanExpiredBufferFilesAsync(string bufferPath, TimeSpan maxAge)
    {
        await Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(bufferPath)) return;
                var di = new DirectoryInfo(bufferPath);
                var cutoff = DateTime.Now - maxAge;
                foreach (var file in di.EnumerateFiles())
                {
                    if (file.LastWriteTime < cutoff)
                    {
                        try { file.Delete(); } catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                _diagnostics.LogError("Buffer cleanup encountered an error", ex);
            }
        });
    }
}
