using System.Diagnostics;
using ClipForge.Core.Interfaces;

namespace ClipForge.Platform.Linux;

public class LinuxGameDetectionService : IGameDetectionService
{
    public ActiveGameInfo GetActiveWindowInfo()
    {
        var info = new ActiveGameInfo();
        try
        {
            var psi = new ProcessStartInfo
            {
                FileName = "xdotool",
                Arguments = "getactivewindow getwindowname",
                RedirectStandardOutput = true,
                UseShellExecute = false
            };
            using var proc = Process.Start(psi);
            if (proc != null)
            {
                var title = proc.StandardOutput.ReadToEnd().Trim();
                if (!string.IsNullOrEmpty(title))
                {
                    info.Title = title;
                    info.IsGame = !title.Contains("bash", StringComparison.OrdinalIgnoreCase) &&
                                  !title.Contains("ClipForge", StringComparison.OrdinalIgnoreCase);
                }
            }
        }
        catch { }
        return info;
    }
}
