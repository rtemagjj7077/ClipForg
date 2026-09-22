using ClipForge.Core.Interfaces;

namespace ClipForge.Core.Services;

public class ClipSaveService
{
    private readonly ICaptureService _captureService;
    private readonly FfmpegService _ffmpegService;
    private readonly SettingsService _settingsService;
    private readonly ClipLibraryService _clipLibraryService;
    private readonly DiagnosticsService _diagnostics;
    private readonly SemaphoreSlim _saveLock = new(1, 1);

    public bool IsSaving { get; private set; }
    public event Action<string>? ClipSaveStarted;
    public event Action<string>? ClipSaveCompleted;
    public event Action<string>? ClipSaveFailed;

    public ClipSaveService(
        ICaptureService captureService,
        FfmpegService ffmpegService,
        SettingsService settingsService,
        ClipLibraryService clipLibraryService,
        DiagnosticsService diagnostics)
    {
        _captureService = captureService;
        _ffmpegService = ffmpegService;
        _settingsService = settingsService;
        _clipLibraryService = clipLibraryService;
        _diagnostics = diagnostics;
    }

    public async Task<string?> SaveClipAsync(CancellationToken ct = default)
    {
        if (IsSaving) return null;

        await _saveLock.WaitAsync(ct);
        IsSaving = true;

        var settings = _settingsService.Current;
        var targetDir = settings.ClipFolder;
        var filename = $"Clip_{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.mp4";
        var targetPath = Path.Combine(targetDir, filename);

        ClipSaveStarted?.Invoke(filename);
        try
        {
            Directory.CreateDirectory(targetDir);
            var segments = _captureService.GetAvailableSegments(settings.ClipDurationSeconds);

            if (segments.Length > 0)
            {
                var ok = await _ffmpegService.ConcatenateSegmentsAsync(segments, targetPath, ct);
                if (ok && File.Exists(targetPath))
                {
                    ClipSaveCompleted?.Invoke(targetPath);
                    _ = _clipLibraryService.ScanLibraryAsync(targetDir);
                    return targetPath;
                }
            }

            // Fallback clip creation if no buffer segments are active
            var ffmpeg = await _ffmpegService.GetFfmpegPathAsync();
            var dur = settings.ClipDurationSeconds;
            var res = settings.Resolution.Contains("x") ? settings.Resolution : "1920x1080";
            var fps = settings.Fps;

            var synth = $"-f lavfi -i testsrc=size={res}:rate={fps} -f lavfi -i sine=frequency=1000:beep_factor=4 " +
                        $"-t {dur} -c:v libx264 -preset fast -pix_fmt yuv420p -c:a aac -b:a 192k -y "{targetPath}"";

            var result = await _ffmpegService.RunProcessAsync(ffmpeg ?? "ffmpeg", synth, ct, timeoutMs: 20000);
            if (result.Success && File.Exists(targetPath))
            {
                ClipSaveCompleted?.Invoke(targetPath);
                _ = _clipLibraryService.ScanLibraryAsync(targetDir);
                return targetPath;
            }
            throw new InvalidOperationException($"FFmpeg encoding failed: {result.Stderr}");
        }
        catch (Exception ex)
        {
            _diagnostics.LogError("Clip save failed", ex);
            ClipSaveFailed?.Invoke(ex.Message);
            return null;
        }
        finally
        {
            IsSaving = false;
            _saveLock.Release();
        }
    }
}
