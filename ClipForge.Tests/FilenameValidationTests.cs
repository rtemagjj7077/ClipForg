using ClipForge.Core.Models;
using ClipForge.Core.Services;
using Xunit;

namespace ClipForge.Tests;

public class FilenameValidationTests
{
    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("invalid/name")]
    [InlineData("invalid\name")]
    [InlineData("invalid:name")]
    [InlineData("invalid*name")]
    public async Task Rename_RejectsInvalidNames(string invalidName)
    {
        var diag = new DiagnosticsService();
        var ffmpeg = new FfmpegService(diag);
        var storage = new StorageService(diag);
        var thumbs = new ThumbnailService(ffmpeg, storage, diag);
        var lib = new ClipLibraryService(ffmpeg, thumbs, diag, _ => { });

        var clip = new ClipItem { FilePath = Path.Combine(Path.GetTempPath(), "test.mp4") };
        var res = await lib.RenameClipAsync(clip, invalidName);
        Assert.False(res.Success);
    }
}
