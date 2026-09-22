using System.Runtime.InteropServices;

namespace ClipForge.Core.Models;

public class AppSettings
{
    // Recording
    public string CaptureMode { get; set; } = "Rolling Buffer";
    public int ClipDurationSeconds { get; set; } = 30;
    public int Fps { get; set; } = 60; // 30, 60, 120, 144, 165, 240
    public string Quality { get; set; } = "Balanced"; // Low, Balanced, High, Custom
    public string Resolution { get; set; } = "1920x1080";
    public int BitrateKbps { get; set; } = 18000;
    public string Encoder { get; set; } = "Auto (GPU Recommended)";
    public string Codec { get; set; } = "H.264 / AVC";
    public string Gpu { get; set; } = "Auto Detect";
    public bool GameCaptureEnabled { get; set; } = true;
    public bool DesktopCaptureEnabled { get; set; } = true;

    // Hotkeys
    public string SaveClipHotkey { get; set; } = "F8";
    public string StartStopHotkey { get; set; } = "F9";
    public string ScreenshotHotkey { get; set; } = "F10";

    // Audio
    public bool GameAudioEnabled { get; set; } = true;
    public double GameAudioVolume { get; set; } = 1.0;
    public bool MicrophoneEnabled { get; set; } = false;
    public double MicrophoneVolume { get; set; } = 0.8;
    public string AudioDevice { get; set; } = "Default System Audio";
    public string MicrophoneDevice { get; set; } = "Default Microphone";
    public bool SeparateAudioTracks { get; set; } = false;

    // Storage
    public string ClipFolder { get; set; } = GetDefaultClipsFolder();
    public string BufferFolder { get; set; } = GetDefaultBufferFolder();
    public int MaxDiskStorageGb { get; set; } = 50;
    public bool AutoCleanupEnabled { get; set; } = true;

    // Appearance
    public string Theme { get; set; } = "Dark";
    public string Accent { get; set; } = "#E5B84A";
    public bool AnimationsEnabled { get; set; } = true;
    public bool CompactMode { get; set; } = false;

    // General
    public bool StartWithWindows { get; set; } = false;
    public bool RunInTray { get; set; } = true;
    public bool NotificationsEnabled { get; set; } = true;

    // Advanced
    public bool HardwareAcceleration { get; set; } = true;
    public bool LoggingEnabled { get; set; } = true;

    public static string GetDefaultClipsFolder()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyVideos), "ClipForge");
        }
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, "Videos", "ClipForge");
    }

    public static string GetDefaultBufferFolder()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "ClipForge", "buffer");
        }
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        return Path.Combine(home, ".local", "share", "ClipForge", "buffer");
    }

    public static AppSettings CreateDefault() => new();
}
