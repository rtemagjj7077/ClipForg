using System.Diagnostics;
using ClipForge.Core.Interfaces;

namespace ClipForge.Platform.Linux;

public class LinuxTrayService : ITrayService
{
    public event Action? ShowRequested;
    public event Action? SaveClipRequested;
    public event Action? StartCaptureRequested;
    public event Action? StopCaptureRequested;
    public event Action? ClipsPageRequested;
    public event Action? SettingsPageRequested;
    public event Action? ExitRequested;

    public void Initialize(string iconPath) { }

    public void ShowNotification(string title, string message)
    {
        try
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = "notify-send",
                Arguments = $"-a "ClipForge" "{title}" "{message}"",
                UseShellExecute = false
            });
        }
        catch { }
    }

    public void Dispose() { }
}
