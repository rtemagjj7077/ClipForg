using System.Runtime.InteropServices;
using ClipForge.Core.Interfaces;

namespace ClipForge.Platform.Windows;

public class WindowsHotkeyService : IHotkeyService
{
    public event Action? SaveClipTriggered;
    public event Action? StartStopTriggered;
    public event Action? ScreenshotTriggered;

    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    public void Initialize() { }

    public void RegisterHotkeys(string saveClip, string startStop, string screenshot)
    {
        // F8 = 0x77, F9 = 0x78, F10 = 0x79
        try
        {
            RegisterHotKey(IntPtr.Zero, 1, 0x4000, 0x77);
            RegisterHotKey(IntPtr.Zero, 2, 0x4000, 0x78);
            RegisterHotKey(IntPtr.Zero, 3, 0x4000, 0x79);
        }
        catch { }
    }

    public void UnregisterHotkeys()
    {
        try
        {
            UnregisterHotKey(IntPtr.Zero, 1);
            UnregisterHotKey(IntPtr.Zero, 2);
            UnregisterHotKey(IntPtr.Zero, 3);
        }
        catch { }
    }

    public void Dispose() => UnregisterHotkeys();
}
