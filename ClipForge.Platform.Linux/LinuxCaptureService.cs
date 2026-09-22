using System.Diagnostics;
using ClipForge.Core.Interfaces;
using ClipForge.Core.Models;
using ClipForge.Core.Services;

namespace ClipForge.Platform.Linux;

public class LinuxCaptureService : ICaptureService
{
    private readonly SettingsService _settingsService;
    private readonly StorageService _storageService;
    private readonly FfmpegService _ffmpegService;
    private readonly DiagnosticsService _diagnostics;
    private Process? _captureProcess;
    private CancellationTokenSource? _cleanupCts;
    private DateTime _captureStartTime;

    public CaptureState State { get; private set; } = CaptureState.Idle;
    public int CurrentFps { get; private set; } = 60;
    public TimeSpan CurrentDuration => State != CaptureState.Idle ? DateTime.Now - _captureStartTime : TimeSpan.Zero;

    public event Action<CaptureState>? StateChanged;
    public event Action<int>? FpsChanged;

    public LinuxCaptureService(
        SettingsService settingsService,
        StorageService storageService,
        FfmpegService ffmpegService,
        DiagnosticsService diagnostics)
    {
        _settingsService = settingsService;
        _storageService = storageService;
        _ffmpegService = ffmpegService;
        _diagnostics = diagnostics;
        CurrentFps = settingsService.Current.Fps;
    }

    public async Task<bool> StartCaptureAsync()
    {
        if (State != CaptureState.Idle) return true;
        try
        {
            var settings = _settingsService.Current;
            CurrentFps = settings.Fps;
            FpsChanged?.Invoke(CurrentFps);
            Directory.CreateDirectory(settings.BufferFolder);

            await _storageService.CleanExpiredBufferFilesAsync(settings.BufferFolder, TimeSpan.Zero);

            var ffmpeg = await _ffmpegService.GetFfmpegPathAsync();
            var segmentPattern = Path.Combine(settings.BufferFolder, "seg_%05d.ts");
            var res = settings.Resolution.Contains("x") ? settings.Resolution : "1920x1080";
            var display = Environment.GetEnvironmentVariable("DISPLAY") ?? ":1";

            // Linux x11grab screen capture
            var args = $"-f x11grab -framerate {settings.Fps} -video_size {res} -i {display} " +
                       $"-c:v libx264 -preset ultrafast -tune zerolatency -pix_fmt yuv420p " +
                       $"-f segment -segment_time 2 -segment_format mpegts -reset_timestamps 1 -y "{segmentPattern}"";

            var psi = new ProcessStartInfo
            {
                FileName = ffmpeg ?? "ffmpeg",
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardError = true,
                RedirectStandardOutput = true
            };

            _captureStartTime = DateTime.Now;
            State = CaptureState.Buffering;
            StateChanged?.Invoke(State);

            try
            {
                _captureProcess = Process.Start(psi);
                _captureProcess?.BeginErrorReadLine();
            }
            catch (Exception ex)
            {
                _diagnostics.LogWarning($"Linux x11grab fallback: {ex.Message}");
            }

            _cleanupCts = new CancellationTokenSource();
            StartBufferCleanerLoop(_cleanupCts.Token);
            return true;
        }
        catch (Exception ex)
        {
            _diagnostics.LogError("Failed to start Linux capture buffer", ex);
            State = CaptureState.Idle;
            StateChanged?.Invoke(State);
            return false;
        }
    }

    public async Task StopCaptureAsync()
    {
        if (State == CaptureState.Idle) return;
        _cleanupCts?.Cancel();
        if (_captureProcess != null && !_captureProcess.HasExited)
        {
            try
            {
                _captureProcess.Kill(true);
                await _captureProcess.WaitForExitAsync();
            }
            catch { }
            finally { _captureProcess.Dispose(); _captureProcess = null; }
        }
        State = CaptureState.Idle;
        StateChanged?.Invoke(State);
    }

    public string[] GetAvailableSegments(int durationSeconds)
    {
        try
        {
            var bufferDir = _settingsService.Current.BufferFolder;
            if (!Directory.Exists(bufferDir)) return Array.Empty<string>();
            var count = (int)Math.Ceiling(durationSeconds / 2.0) + 1;
            return new DirectoryInfo(bufferDir)
                .EnumerateFiles("seg_*.ts")
                .OrderByDescending(f => f.LastWriteTime)
                .Take(count)
                .OrderBy(f => f.LastWriteTime)
                .Select(f => f.FullName)
                .ToArray();
        }
        catch { return Array.Empty<string>(); }
    }

    private void StartBufferCleanerLoop(CancellationToken ct)
    {
        Task.Run(async () =>
        {
            while (!ct.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(2000, ct);
                    var bufferDir = _settingsService.Current.BufferFolder;
                    if (Directory.Exists(bufferDir))
                    {
                        var maxAge = TimeSpan.FromSeconds(_settingsService.Current.ClipDurationSeconds + 10);
                        var cutoff = DateTime.Now - maxAge;
                        foreach (var f in new DirectoryInfo(bufferDir).EnumerateFiles("seg_*.ts"))
                        {
                            if (f.LastWriteTime < cutoff) try { f.Delete(); } catch { }
                        }
                    }
                }
                catch (OperationCanceledException) { break; }
                catch { }
            }
        }, ct);
    }
}
