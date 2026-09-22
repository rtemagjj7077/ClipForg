using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace ClipForge.Core.Services;

public class ThumbnailService
{
    private readonly FfmpegService _ffmpegService;
    private readonly StorageService _storageService;
    private readonly DiagnosticsService _diagnostics;
    private readonly SemaphoreSlim _concurrencyLimiter = new(2, 2);
    private readonly ConcurrentDictionary<string, string> _thumbnailPathCache = new();

    public ThumbnailService(FfmpegService ffmpegService, StorageService storageService, DiagnosticsService diagnostics)
    {
        _ffmpegService = ffmpegService;
        _storageService = storageService;
        _diagnostics = diagnostics;
    }

    public string GetDeterministicCacheKey(string filePath, long fileSize, DateTime lastModified)
    {
        var raw = $"{filePath.ToLowerInvariant()}_{fileSize}_{lastModified.Ticks}";
        using var sha = SHA256.Create();
        var hash = sha.ComputeHash(Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(hash).Substring(0, 24);
    }

    public async Task<string?> GetThumbnailPathAsync(string videoPath, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(videoPath) || !File.Exists(videoPath)) return null;

        try
        {
            var fi = new FileInfo(videoPath);
            var key = GetDeterministicCacheKey(videoPath, fi.Length, fi.LastWriteTimeUtc);

            if (_thumbnailPathCache.TryGetValue(key, out var cached) && File.Exists(cached))
                return cached;

            var diskThumbPath = Path.Combine(_storageService.ThumbnailsDirectory, $"{key}.jpg");
            if (File.Exists(diskThumbPath))
            {
                _thumbnailPathCache[key] = diskThumbPath;
                return diskThumbPath;
            }

            await _concurrencyLimiter.WaitAsync(ct);
            try
            {
                if (File.Exists(diskThumbPath))
                {
                    _thumbnailPathCache[key] = diskThumbPath;
                    return diskThumbPath;
                }

                var success = await _ffmpegService.ExtractThumbnailAsync(videoPath, diskThumbPath, TimeSpan.FromSeconds(1), ct);
                if (!success || !File.Exists(diskThumbPath))
                {
                    await _ffmpegService.ExtractThumbnailAsync(videoPath, diskThumbPath, TimeSpan.FromMilliseconds(100), ct);
                }

                if (File.Exists(diskThumbPath))
                {
                    _thumbnailPathCache[key] = diskThumbPath;
                    return diskThumbPath;
                }
            }
            finally
            {
                _concurrencyLimiter.Release();
            }
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _diagnostics.LogWarning($"Thumbnail skipped for {videoPath}: {ex.Message}");
        }
        return null;
    }
}
