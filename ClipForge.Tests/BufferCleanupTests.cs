using ClipForge.Core.Services;
using Xunit;

namespace ClipForge.Tests;

public class BufferCleanupTests
{
    [Fact]
    public async Task CleanExpiredBufferFiles_RemovesOldFilesAndRetainsRecent()
    {
        var diag = new DiagnosticsService();
        var storage = new StorageService(diag);
        var tempBufferDir = Path.Combine(Path.GetTempPath(), $"buffer_test_{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempBufferDir);

        try
        {
            var oldFile = Path.Combine(tempBufferDir, "segment_old.ts");
            var newFile = Path.Combine(tempBufferDir, "segment_new.ts");

            File.WriteAllText(oldFile, "dummy old segment");
            File.WriteAllText(newFile, "dummy new segment");

            File.SetLastWriteTime(oldFile, DateTime.Now.AddHours(-2));
            File.SetLastWriteTime(newFile, DateTime.Now);

            await storage.CleanExpiredBufferFilesAsync(tempBufferDir, TimeSpan.FromHours(1));

            Assert.False(File.Exists(oldFile), "Old buffer segment should have been cleaned up.");
            Assert.True(File.Exists(newFile), "New buffer segment should have been retained.");
        }
        finally
        {
            if (Directory.Exists(tempBufferDir))
                Directory.Delete(tempBufferDir, recursive: true);
        }
    }
}
