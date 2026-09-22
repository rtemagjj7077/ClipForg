namespace ClipForge.Core.Interfaces;

public class ActiveGameInfo
{
    public string Title { get; set; } = "Desktop";
    public string ProcessName { get; set; } = "explorer";
    public bool IsGame { get; set; } = false;
    public IntPtr WindowId { get; set; } = IntPtr.Zero;
}

public interface IGameDetectionService
{
    ActiveGameInfo GetActiveWindowInfo();
}
