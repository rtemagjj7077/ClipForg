using ClipForge.Core.Services;
using Xunit;

namespace ClipForge.Tests;

public class ThumbnailCacheTests
{
    [Fact]
    public void CacheKey_IsDeterministic()
    {
        var diag = new DiagnosticsService();
        var ffmpeg = new FfmpegService(diag);
        var storage = new StorageService(diag);
        var thumbs = new ThumbnailService(ffmpeg, storage, diag);

        var key1 = thumbs.GetDeterministicCacheKey("/path/video.mp4", 1024, new DateTime(2026, 1, 1));
        var key2 = thumbs.GetDeterministicCacheKey("/path/video.mp4", 1024, new DateTime(2026, 1, 1));
        var key3 = thumbs.GetDeterministicCacheKey("/path/other.mp4", 1024, new DateTime(2026, 1, 1));

        Assert.Equal(key1, key2);
        Assert.NotEqual(key1, key3);
    }
}
