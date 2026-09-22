using ClipForge.Core.Interfaces;

namespace ClipForge.Platform.Linux;

public class LinuxHotkeyService : IHotkeyService
{
    public event Action? SaveClipTriggered;
    public event Action? StartStopTriggered;
    public event Action? ScreenshotTriggered;

    public void Initialize() { }
    public void RegisterHotkeys(string saveClip, string startStop, string screenshot) { }
    public void UnregisterHotkeys() { }
    public void Dispose() { }
}
