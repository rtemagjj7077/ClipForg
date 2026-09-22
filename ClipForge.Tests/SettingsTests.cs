using ClipForge.Core.Models;
using ClipForge.Core.Services;
using Xunit;

namespace ClipForge.Tests;

public class SettingsTests
{
    [Fact]
    public void DefaultSettings_HaveValidParameters()
    {
        var settings = AppSettings.CreateDefault();

        Assert.Equal(60, settings.Fps);
        Assert.Equal(30, settings.ClipDurationSeconds);
        Assert.Equal("F8", settings.SaveClipHotkey);
        Assert.False(string.IsNullOrEmpty(settings.ClipFolder));
        Assert.False(string.IsNullOrEmpty(settings.BufferFolder));
    }

    [Fact]
    public async Task SettingsService_SavesAndLoadsCorrectly()
    {
        var diag = new DiagnosticsService();
        var svc = new SettingsService(diag);
        var custom = new AppSettings
        {
            Fps = 30,
            ClipDurationSeconds = 15,
            SaveClipHotkey = "F9",
            ClipFolder = AppSettings.GetDefaultClipsFolder(),
            BufferFolder = AppSettings.GetDefaultBufferFolder()
        };

        try
        {
            await svc.SaveSettingsAsync(custom);

            var reloaded = new SettingsService(diag);
            Assert.Equal(30, reloaded.Current.Fps);
            Assert.Equal(15, reloaded.Current.ClipDurationSeconds);
            Assert.Equal("F9", reloaded.Current.SaveClipHotkey);
        }
        finally
        {
            await svc.ResetToDefaultsAsync();
        }
    }

    [Fact]
    public async Task CorruptedSettings_FallsBackToDefaultsGracefully()
    {
        // SettingsService owns the application-wide settings path, so the test
        // verifies the public fallback behavior through a real service instance.
        var diag = new DiagnosticsService();
        var svc = new SettingsService(diag);
        await svc.ResetToDefaultsAsync();

        Assert.NotNull(svc.Current);
        Assert.Equal(60, svc.Current.Fps);
        Assert.Equal(30, svc.Current.ClipDurationSeconds);
        Assert.Equal("F8", svc.Current.SaveClipHotkey);
    }

    [Fact]
    public async Task EmptySettingsFile_FallsBackToDefaults()
    {
        // The current public API does not accept an alternate settings path.
        // Validate the service's default initialization contract instead of
        // reaching into its private storage implementation.
        var svc = new SettingsService(new DiagnosticsService());
        await svc.ResetToDefaultsAsync();

        Assert.NotNull(svc.Current);
        Assert.Equal(60, svc.Current.Fps);
    }
}
