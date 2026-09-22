using System.Diagnostics;
using ClipForge.Core.Interfaces;

namespace ClipForge.Platform.Windows;

public class WindowsMediaPlayerService : IMediaPlayerService
{
    public Task<bool> VerifyMediaPlayableAsync(string filePath)
    {
        return Task.FromResult(File.Exists(filePath) && new FileInfo(filePath).Length > 0);
    }

    public void OpenWithSystemDefault(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = filePath, UseShellExecute = true });
        }
        catch { }
    }

    public void ShowInFileBrowser(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "explorer.exe",
                Arguments = $"/select,"{filePath}"",
                UseShellExecute = true
            });
        }
        catch { }
    }
}
