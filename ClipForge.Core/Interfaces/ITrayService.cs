namespace ClipForge.Core.Interfaces;

public interface ITrayService : IDisposable
{
    event Action? ShowRequested;
    event Action? SaveClipRequested;
    event Action? StartCaptureRequested;
    event Action? StopCaptureRequested;
    event Action? ClipsPageRequested;
    event Action? SettingsPageRequested;
    event Action? ExitRequested;

    void Initialize(string iconPath);
    void ShowNotification(string title, string message);
}
