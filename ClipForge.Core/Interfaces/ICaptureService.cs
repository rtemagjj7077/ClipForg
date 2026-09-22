namespace ClipForge.Core.Interfaces;

public enum CaptureState
{
    Idle,
    Buffering,
    Recording,
    Paused
}

public interface ICaptureService
{
    CaptureState State { get; }
    int CurrentFps { get; }
    TimeSpan CurrentDuration { get; }
    event Action<CaptureState>? StateChanged;
    event Action<int>? FpsChanged;

    Task<bool> StartCaptureAsync();
    Task StopCaptureAsync();
    string[] GetAvailableSegments(int durationSeconds);
}
