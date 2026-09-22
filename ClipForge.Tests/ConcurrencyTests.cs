using ClipForge.Core.Models;
using ClipForge.Core.Services;
using Xunit;

namespace ClipForge.Tests;

public class ConcurrencyTests
{
    [Fact]
    public async Task ConcurrentSettingsUpdates_DoNotCorruptSettingsFile()
    {
        var diag = new DiagnosticsService();
        var tempFile = Path.Combine(Path.GetTempPath(), $"settings_concurrency_{Guid.NewGuid():N}.json");
        try
        {
            var svc = new SettingsService(diag, tempFile);
            var tasks = new List<Task>();

            for (int i = 0; i < 20; i++)
            {
                var val = i;
                tasks.Add(Task.Run(async () =>
                {
                    await svc.SaveSettingsAsync(new AppSettings
                    {
                        ClipDurationSeconds = 10 + val,
                        Fps = 30 + val
                    });
                }));
            }

            await Task.WhenAll(tasks);

            var reloaded = new SettingsService(diag, tempFile);
            Assert.NotNull(reloaded.Current);
            Assert.True(reloaded.Current.ClipDurationSeconds >= 10);
        }
        finally
        {
            if (File.Exists(tempFile)) File.Delete(tempFile);
        }
    }

    [Fact]
    public async Task ConcurrentFolderSizeCalculations_ExecuteWithoutDeadlock()
    {
        var diag = new DiagnosticsService();
        var storage = new StorageService(diag);
        var tempDir = Path.Combine(Path.GetTempPath(), $"storage_concurrency_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);

        try
        {
            for (int i = 0; i < 5; i++)
            {
                File.WriteAllText(Path.Combine(tempDir, $"f_{i}.dat"), "test content");
            }

            var tasks = Enumerable.Range(0, 10).Select(_ => storage.GetFolderSizeBytesAsync(tempDir)).ToList();
            var results = await Task.WhenAll(tasks);

            Assert.All(results, size => Assert.True(size > 0));
        }
        finally
        {
            if (Directory.Exists(tempDir)) Directory.Delete(tempDir, recursive: true);
        }
    }
}
