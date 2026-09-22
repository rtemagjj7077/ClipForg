using System.Xml.Linq;
using Xunit;

namespace ClipForge.Tests;

public class WindowsBuildValidationTests
{
    [Fact]
    public void WindowsProject_ConfiguredAsWinExe()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ClipForge.sln")))
        {
            dir = dir.Parent;
        }

        if (dir != null)
        {
            var winCsprojPath = Path.Combine(dir.FullName, "ClipForge.Platform.Windows", "ClipForge.Platform.Windows.csproj");
            if (File.Exists(winCsprojPath))
            {
                var doc = XDocument.Load(winCsprojPath);
                var outputType = doc.Descendants("OutputType").FirstOrDefault()?.Value;
                var assemblyName = doc.Descendants("AssemblyName").FirstOrDefault()?.Value;
                var manifest = doc.Descendants("ApplicationManifest").FirstOrDefault()?.Value;

                Assert.Equal("WinExe", outputType);
                Assert.Equal("ClipForge", assemblyName);
                Assert.Equal("app.manifest", manifest);
            }
        }
    }

    [Fact]
    public void WindowsManifest_ContainsDpiAndCompatibilitySettings()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var dir = new DirectoryInfo(baseDir);
        while (dir != null && !File.Exists(Path.Combine(dir.FullName, "ClipForge.sln")))
        {
            dir = dir.Parent;
        }

        if (dir != null)
        {
            var manifestPath = Path.Combine(dir.FullName, "ClipForge.Platform.Windows", "app.manifest");
            if (File.Exists(manifestPath))
            {
                var content = File.ReadAllText(manifestPath);
                Assert.Contains("dpiAware", content);
                Assert.Contains("{8e0f7a12-bfb3-4fe8-b9a5-48fd50a15a9a}", content);
            }
        }
    }
}
