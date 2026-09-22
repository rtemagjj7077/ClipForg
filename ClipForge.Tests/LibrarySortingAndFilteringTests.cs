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

    [Fact]
    public void Sorting_OrdersClipsCorrectly()
    {
        var lib = CreateService();
        var c1 = new ClipItem { FileName = "Beta.mp4", CreatedAt = DateTime.Now.AddMinutes(-10), Duration = TimeSpan.FromSeconds(20), FileSize = 200 };
        var c2 = new ClipItem { FileName = "Alpha.mp4", CreatedAt = DateTime.Now, Duration = TimeSpan.FromSeconds(60), FileSize = 100 };
        var c3 = new ClipItem { FileName = "Gamma.mp4", CreatedAt = DateTime.Now.AddMinutes(-20), Duration = TimeSpan.FromSeconds(40), FileSize = 300 };

        lib.AddClipForTesting(c1);
        lib.AddClipForTesting(c2);
        lib.AddClipForTesting(c3);

        // NameAsc
        lib.SetSortOrder(ClipSortOrder.NameAsc);
        Assert.Equal("Alpha.mp4", lib.VisibleClips[0].FileName);
        Assert.Equal("Beta.mp4", lib.VisibleClips[1].FileName);
        Assert.Equal("Gamma.mp4", lib.VisibleClips[2].FileName);

        // NameDesc
        lib.SetSortOrder(ClipSortOrder.NameDesc);
        Assert.Equal("Gamma.mp4", lib.VisibleClips[0].FileName);

        // DateAsc
        lib.SetSortOrder(ClipSortOrder.DateAsc);
        Assert.Equal("Gamma.mp4", lib.VisibleClips[0].FileName);

        // DateDesc
        lib.SetSortOrder(ClipSortOrder.DateDesc);
        Assert.Equal("Alpha.mp4", lib.VisibleClips[0].FileName);

        // DurationDesc
        lib.SetSortOrder(ClipSortOrder.DurationDesc);
        Assert.Equal("Alpha.mp4", lib.VisibleClips[0].FileName);

        // SizeDesc
        lib.SetSortOrder(ClipSortOrder.SizeDesc);
        Assert.Equal("Gamma.mp4", lib.VisibleClips[0].FileName);
    }

    [Fact]
    public void SearchQuery_FiltersClipsBySubstring()
    {
        var lib = CreateService();
        lib.AddClipForTesting(new ClipItem { FileName = "Overwatch_Clip1.mp4", CreatedAt = DateTime.Now });
        lib.AddClipForTesting(new ClipItem { FileName = "Minecraft_Build.mp4", CreatedAt = DateTime.Now });
        lib.AddClipForTesting(new ClipItem { FileName = "Overwatch_PotG.mp4", CreatedAt = DateTime.Now });

        lib.SetSearchQuery("overwatch");
        Assert.Equal(2, lib.VisibleClips.Count);
        Assert.All(lib.VisibleClips, c => Assert.Contains("Overwatch", c.FileName, StringComparison.OrdinalIgnoreCase));

        lib.SetSearchQuery("minecraft");
        Assert.Single(lib.VisibleClips);
        Assert.Equal("Minecraft_Build.mp4", lib.VisibleClips[0].FileName);

        lib.SetSearchQuery("");
        Assert.Equal(3, lib.VisibleClips.Count);
    }

    [Fact]
    public void DurationFilter_FiltersByTimeRanges()
    {
        var lib = CreateService();
        lib.AddClipForTesting(new ClipItem { FileName = "Short.mp4", Duration = TimeSpan.FromSeconds(15), CreatedAt = DateTime.Now });
        lib.AddClipForTesting(new ClipItem { FileName = "Medium.mp4", Duration = TimeSpan.FromSeconds(45), CreatedAt = DateTime.Now });
        lib.AddClipForTesting(new ClipItem { FileName = "Long.mp4", Duration = TimeSpan.FromSeconds(90), CreatedAt = DateTime.Now });

        lib.SetDurationFilter("<30s");
        Assert.Single(lib.VisibleClips);
        Assert.Equal("Short.mp4", lib.VisibleClips[0].FileName);

        lib.SetDurationFilter("30s-60s");
        Assert.Single(lib.VisibleClips);
        Assert.Equal("Medium.mp4", lib.VisibleClips[0].FileName);

        lib.SetDurationFilter(">60s");
        Assert.Single(lib.VisibleClips);
        Assert.Equal("Long.mp4", lib.VisibleClips[0].FileName);

        lib.SetDurationFilter(null);
        Assert.Equal(3, lib.VisibleClips.Count);
    }
}
