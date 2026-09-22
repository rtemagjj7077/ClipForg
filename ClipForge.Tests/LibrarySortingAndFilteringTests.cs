using ClipForge.Core.Models;
using ClipForge.Core.Services;
using Xunit;

namespace ClipForge.Tests;

public class LibrarySortingAndFilteringTests
{
    private ClipLibraryService CreateService()
    {
        var diag = new DiagnosticsService();
        var ffmpeg = new FfmpegService(diag);
        var storage = new StorageService(diag);
        var thumbs = new ThumbnailService(ffmpeg, storage, diag);
        return new ClipLibraryService(ffmpeg, thumbs, diag, action => action());
    }

    private static string CreateClipFolder(params (string Name, int Size)[] clips)
    {
        var folder = Path.Combine(Path.GetTempPath(), $"clip_library_{Guid.NewGuid():N}");
        Directory.CreateDirectory(folder);

        foreach (var (name, size) in clips)
        {
            var path = Path.Combine(folder, name);
            File.WriteAllBytes(path, new byte[size]);
        }

        return folder;
    }

    [Fact]
    public async Task Sorting_OrdersScannedClipsCorrectly()
    {
        var folder = CreateClipFolder(
            ("Beta.mp4", 200),
            ("Alpha.mp4", 100),
            ("Gamma.mp4", 300));

        try
        {
            var lib = CreateService();
            await lib.ScanLibraryAsync(folder);

            lib.SetSortOrder(ClipSortOrder.NameAsc);
            Assert.Equal(new[] { "Alpha.mp4", "Beta.mp4", "Gamma.mp4" },
                lib.VisibleClips.Select(c => c.FileName));

            lib.SetSortOrder(ClipSortOrder.NameDesc);
            Assert.Equal("Gamma.mp4", lib.VisibleClips[0].FileName);

            lib.SetSortOrder(ClipSortOrder.SizeDesc);
            Assert.Equal("Gamma.mp4", lib.VisibleClips[0].FileName);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task SearchQuery_FiltersScannedClipsBySubstring()
    {
        var folder = CreateClipFolder(
            ("Overwatch_Clip1.mp4", 1),
            ("Minecraft_Build.mp4", 1),
            ("Overwatch_PotG.mp4", 1));

        try
        {
            var lib = CreateService();
            await lib.ScanLibraryAsync(folder);

            lib.SetSearchQuery("overwatch");
            Assert.Equal(2, lib.VisibleClips.Count);
            Assert.All(lib.VisibleClips, c =>
                Assert.Contains("Overwatch", c.FileName, StringComparison.OrdinalIgnoreCase));

            lib.SetSearchQuery("minecraft");
            Assert.Single(lib.VisibleClips);
            Assert.Equal("Minecraft_Build.mp4", lib.VisibleClips[0].FileName);

            lib.SetSearchQuery("");
            Assert.Equal(3, lib.VisibleClips.Count);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public async Task DurationFilter_UsesDurationsReportedByLibraryScan()
    {
        var folder = CreateClipFolder(
            ("First.mp4", 1),
            ("Second.mp4", 1),
            ("Third.mp4", 1));

        try
        {
            var lib = CreateService();
            await lib.ScanLibraryAsync(folder);

            // ClipLibraryService assigns its documented 30-second fallback
            // duration during scanning; metadata enrichment runs asynchronously.
            lib.SetDurationFilter("30s-60s");
            Assert.Equal(3, lib.VisibleClips.Count);

            lib.SetDurationFilter("<30s");
            Assert.Empty(lib.VisibleClips);

            lib.SetDurationFilter(">60s");
            Assert.Empty(lib.VisibleClips);

            lib.SetDurationFilter(null);
            Assert.Equal(3, lib.VisibleClips.Count);
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}
