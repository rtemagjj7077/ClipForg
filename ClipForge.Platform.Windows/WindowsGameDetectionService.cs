using System.Runtime.InteropServices;
using System.Text;
using ClipForge.Core.Interfaces;

namespace ClipForge.Platform.Windows;

public class WindowsGameDetectionService : IGameDetectionService
{
    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern int GetWindowText(IntPtr hWnd, StringBuilder text, int count);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

    public ActiveGameInfo GetActiveWindowInfo()
    {
        var info = new ActiveGameInfo();
        try
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return info;
            info.WindowId = hwnd;

            var sb = new StringBuilder(256);
            if (GetWindowText(hwnd, sb, sb.Capacity) > 0)
                info.Title = sb.ToString();

            GetWindowThreadProcessId(hwnd, out uint pid);
            if (pid > 0)
            {
                var p = System.Diagnostics.Process.GetProcessById((int)pid);
                info.ProcessName = p.ProcessName;
                info.IsGame = !p.ProcessName.Equals("explorer", StringComparison.OrdinalIgnoreCase) &&
                              !p.ProcessName.Equals("ClipForge", StringComparison.OrdinalIgnoreCase);
            }
        }
        catch { }
        return info;
    }
}
