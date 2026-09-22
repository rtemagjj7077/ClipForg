using ClipForge.Core.Models;
using ClipForge.Core.Services;
using System.Text.Json;
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
        Assert.True(settings.BufferDurationSeconds >= settings.ClipDurationSeconds);
    }

    [Fact]
    public async Task SettingsService_SavesAndLoadsCorrectly()
    {
        var diag = new DiagnosticsService();
        var tempFile = Path.Combine(Path.GetTempPath(), $"settings_valid_{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(diag, tempFile);
            var custom = new AppSettings
            {
                Fps = 30,
                ClipDurationSeconds = 15,
                SaveClipHotkey = "F9",
                ClipFolder = Path.GetTempPath()
            };
            await svc.SaveSettingsAsync(custom);

            var svcReloaded = new SettingsService(diag, tempFile);
            Assert.Equal(30, svcReloaded.Current.Fps);
            Assert.Equal(15, svcReloaded.Current.ClipDurationSeconds);
            Assert.Equal("F9", svcReloaded.Current.SaveClipHotkey);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void CorruptedSettings_FallsBackToDefaultsGracefully()
    {
        var diag = new DiagnosticsService();
        var tempFile = Path.Combine(Path.GetTempPath(), $"settings_corrupt_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempFile, "{ this is not valid json : [[ [ ");
            var svc = new SettingsService(diag, tempFile);
            Assert.NotNull(svc.Current);
            Assert.Equal(60, svc.Current.Fps);
            Assert.Equal(30, svc.Current.ClipDurationSeconds);
            Assert.Equal("F8", svc.Current.SaveClipHotkey);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public void EmptySettingsFile_FallsBackToDefaults()
    {
        var diag = new DiagnosticsService();
        var tempFile = Path.Combine(Path.GetTempPath(), $"settings_empty_{Guid.NewGuid():N}.json");
        try
        {
            File.WriteAllText(tempFile, "");
            var svc = new SettingsService(diag, tempFile);
            Assert.NotNull(svc.Current);
            Assert.Equal(60, svc.Current.Fps);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }
}
