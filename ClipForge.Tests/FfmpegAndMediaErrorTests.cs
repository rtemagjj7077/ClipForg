using ClipForge.Core.Services;
using Xunit;

namespace ClipForge.Tests;

public class FfmpegAndMediaErrorTests
{
    [Fact]
    public async Task CorruptOrZeroByteMedia_ReturnsDefaultMetadataWithoutCrashing()
    {
        var diag = new DiagnosticsService();
        var ffmpeg = new FfmpegService(diag);
        var tempFile = Path.Combine(Path.GetTempPath(), $"corrupt_media_{Guid.NewGuid():N}.mp4");

        try
        {
            File.WriteAllBytes(tempFile, Array.Empty<byte>());

            var meta = await ffmpeg.GetVideoMetadataAsync(tempFile);
            Assert.NotNull(meta);
            Assert.Equal(1920, meta.Width);
            Assert.Equal(1080, meta.Height);
            Assert.Equal(60, meta.Fps);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task NonExistentMediaFile_ReturnsDefaultMetadata()
    {
        var diag = new DiagnosticsService();
        var ffmpeg = new FfmpegService(diag);
        var nonexistent = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.mp4");

        var meta = await ffmpeg.GetVideoMetadataAsync(nonexistent);
        Assert.NotNull(meta);
        Assert.Equal(1920, meta.Width);
    }

    [Fact]
    public async Task ConcatenateSegments_WithEmptyList_ReturnsFalseGracefully()
    {
        var diag = new DiagnosticsService();
        var ffmpeg = new FfmpegService(diag);
        var outPath = Path.Combine(Path.GetTempPath(), $"concat_out_{Guid.NewGuid():N}.mp4");

        var success = await ffmpeg.ConcatenateSegmentsAsync(Array.Empty<string>(), outPath);
        Assert.False(success);
    }

    [Fact]
    public async Task ExtractThumbnail_OnNonExistentMedia_ReturnsFalse()
    {
        var diag = new DiagnosticsService();
        var ffmpeg = new FfmpegService(diag);
        var fakeVideo = Path.Combine(Path.GetTempPath(), $"missing_{Guid.NewGuid():N}.mp4");
        var outThumb = Path.Combine(Path.GetTempPath(), $"thumb_{Guid.NewGuid():N}.jpg");

        var success = await ffmpeg.ExtractThumbnailAsync(fakeVideo, outThumb, TimeSpan.FromSeconds(1));
        Assert.False(success);
        Assert.False(File.Exists(outThumb));
    }
}
