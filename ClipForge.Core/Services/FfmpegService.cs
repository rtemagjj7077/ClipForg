using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;

namespace ClipForge.Core.Services;

public class VideoMetadata
{
    public TimeSpan Duration { get; set; } = TimeSpan.FromSeconds(30);
    public int Width { get; set; } = 1920;
    public int Height { get; set; } = 1080;
    public int Fps { get; set; } = 60;
}

public class FfmpegService
{
    private readonly DiagnosticsService _diagnostics;
    private string? _cachedFfmpegPath;

    public FfmpegService(DiagnosticsService diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public async Task<string?> GetFfmpegPathAsync()
    {
        if (_cachedFfmpegPath != null) return _cachedFfmpegPath;

        return await Task.Run(() =>
        {
            var isWin = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            var exeName = isWin ? "ffmpeg.exe" : "ffmpeg";

            var candidates = new List<string>
            {
                Path.Combine(AppDomain.CurrentDomain.BaseDirectory, exeName),
                Path.Combine(StorageService.GetDefaultDataDirectory(), "bin", exeName),
                exeName
            };

            if (!isWin)
            {
                candidates.Add("/usr/bin/ffmpeg");
                candidates.Add("/usr/local/bin/ffmpeg");
            }

            foreach (var cand in candidates)
            {
                try
                {
                    if (File.Exists(cand))
                    {
                        _cachedFfmpegPath = Path.GetFullPath(cand);
                        return _cachedFfmpegPath;
                    }
                }
                catch { }
            }

            _cachedFfmpegPath = "ffmpeg";
            return _cachedFfmpegPath;
        });
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var ffmpeg = await GetFfmpegPathAsync();
            var res = await RunProcessAsync(ffmpeg ?? "ffmpeg", "-version", timeoutMs: 3000);
            return res.Success;
        }
        catch
        {
            return false;
        }
    }

    public async Task<bool> ExtractThumbnailAsync(string videoPath, string outputPath, TimeSpan position, CancellationToken ct = default)
    {
        try
        {
            var ffmpeg = await GetFfmpegPathAsync();
            var ss = position.TotalSeconds.ToString("0.###", CultureInfo.InvariantCulture);
            var args = $"-ss {ss} -i "{videoPath}" -vframes 1 -vf "scale=480:270:force_original_aspect_ratio=decrease,pad=480:270:(ow-iw)/2:(oh-ih)/2:black" -q:v 3 -y "{outputPath}"";
            var result = await RunProcessAsync(ffmpeg ?? "ffmpeg", args, ct);
            return result.Success && File.Exists(outputPath);
        }
        catch (Exception ex)
        {
            _diagnostics.LogError($"Failed thumbnail for {videoPath}", ex);
            return false;
        }
    }

    public async Task<VideoMetadata> GetVideoMetadataAsync(string videoPath, CancellationToken ct = default)
    {
        var metadata = new VideoMetadata();
        try
        {
            var ffmpeg = await GetFfmpegPathAsync();
            var result = await RunProcessAsync(ffmpeg ?? "ffmpeg", $"-i "{videoPath}"", ct, timeoutMs: 5000);
            var output = result.Stderr;

            var matchDuration = Regex.Match(output, @"Duration:\s*(\d+):(\d+):(\d+\.?\d*)");
            if (matchDuration.Success)
            {
                var h = int.Parse(matchDuration.Groups[1].Value);
                var m = int.Parse(matchDuration.Groups[2].Value);
                var s = double.Parse(matchDuration.Groups[3].Value, CultureInfo.InvariantCulture);
                metadata.Duration = TimeSpan.FromSeconds(h * 3600 + m * 60 + s);
            }

            var matchRes = Regex.Match(output, @"(\d{3,4})x(\d{3,4})");
            if (matchRes.Success)
            {
                metadata.Width = int.Parse(matchRes.Groups[1].Value);
                metadata.Height = int.Parse(matchRes.Groups[2].Value);
            }

            var matchFps = Regex.Match(output, @"(\d+\.?\d*)\s*fps");
            if (matchFps.Success && double.TryParse(matchFps.Groups[1].Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var fpsVal))
            {
                metadata.Fps = (int)Math.Round(fpsVal);
            }
        }
        catch (Exception ex)
        {
            _diagnostics.LogError($"Failed to probe metadata for {videoPath}", ex);
        }
        return metadata;
    }

    public async Task<bool> ConcatenateSegmentsAsync(string[] segmentFiles, string outputPath, CancellationToken ct = default)
    {
        if (segmentFiles.Length == 0) return false;
        var listFile = Path.Combine(Path.GetDirectoryName(outputPath) ?? Path.GetTempPath(), $"concat_{Guid.NewGuid():N}.txt");
        try
        {
            var sb = new StringBuilder();
            foreach (var seg in segmentFiles)
            {
                var safePath = seg.Replace("\", "/").Replace("'", "'\''");
                sb.AppendLine($"file '{safePath}'");
            }
            await File.WriteAllTextAsync(listFile, sb.ToString(), ct);

            var ffmpeg = await GetFfmpegPathAsync();
            var args = $"-f concat -safe 0 -i "{listFile}" -c copy -movflags +faststart -y "{outputPath}"";
            var result = await RunProcessAsync(ffmpeg ?? "ffmpeg", args, ct, timeoutMs: 30000);
            return result.Success && File.Exists(outputPath);
        }
        finally
        {
            try { if (File.Exists(listFile)) File.Delete(listFile); } catch { }
        }
    }

    public async Task<(bool Success, string Stdout, string Stderr)> RunProcessAsync(
        string executable, string arguments, CancellationToken ct = default, int timeoutMs = 15000)
    {
        var psi = new ProcessStartInfo
        {
            FileName = executable,
            Arguments = arguments,
            UseShellExecute = false,
            CreateNoWindow = true,
            WindowStyle = ProcessWindowStyle.Hidden,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        var stdout = new StringBuilder();
        var stderr = new StringBuilder();
        using var proc = new Process { StartInfo = psi };
        proc.OutputDataReceived += (_, e) => { if (e.Data != null) stdout.AppendLine(e.Data); };
        proc.ErrorDataReceived += (_, e) => { if (e.Data != null) stderr.AppendLine(e.Data); };

        try
        {
            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            using var timeoutCts = new CancellationTokenSource(timeoutMs);
            using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct, timeoutCts.Token);
            await proc.WaitForExitAsync(linked.Token);
            return (proc.ExitCode == 0, stdout.ToString(), stderr.ToString());
        }
        catch (Exception ex)
        {
            try { if (!proc.HasExited) proc.Kill(entireProcessTree: true); } catch { }
            return (false, stdout.ToString(), ex.Message);
        }
    }
}
