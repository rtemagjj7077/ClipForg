using ClipForge.Core.Interfaces;

namespace ClipForge.Platform.Windows;

public class WindowsTrayService : ITrayService
{
    public event Action? ShowRequested;
    public event Action? SaveClipRequested;
    public event Action? StartCaptureRequested;
    public event Action? StopCaptureRequested;
    public event Action? ClipsPageRequested;
    public event Action? SettingsPageRequested;
    public event Action? ExitRequested;

    public void Initialize(string iconPath) { }
    public void ShowNotification(string title, string message) { }
    public void Dispose() { }
}
