using System.Diagnostics;
using ClipForge.Core.Interfaces;

namespace ClipForge.Platform.Linux;

public class LinuxMediaPlayerService : IMediaPlayerService
{
    public Task<bool> VerifyMediaPlayableAsync(string filePath)
    {
        return Task.FromResult(File.Exists(filePath) && new FileInfo(filePath).Length > 0);
    }

    public void OpenWithSystemDefault(string filePath)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "xdg-open",
                Arguments = $""{filePath}"",
                UseShellExecute = false
            });
        }
        catch { }
    }

    public void ShowInFileBrowser(string filePath)
    {
        try
        {
            var dir = Path.GetDirectoryName(filePath);
            if (dir != null)
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = "xdg-open",
                    Arguments = $""{dir}"",
                    UseShellExecute = false
                });
            }
        }
        catch { }
    }
}
