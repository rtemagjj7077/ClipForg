namespace ClipForge.Core.Interfaces;

public interface IHotkeyService : IDisposable
{
    event Action? SaveClipTriggered;
    event Action? StartStopTriggered;
    event Action? ScreenshotTriggered;

    void Initialize();
    void RegisterHotkeys(string saveClip, string startStop, string screenshot);
    void UnregisterHotkeys();
}
