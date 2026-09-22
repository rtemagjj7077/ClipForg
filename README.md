# ClipForge — Cross-Platform Native Gaming Clip Capture

ClipForge is a high-performance cross-platform gaming clip capture application engineered with **C#**, **.NET 8**, and **Avalonia UI**, utilizing hardware-accelerated FFmpeg for rolling buffer recording, streaming concatenation, and asynchronous thumbnail generation.

---

## 🏗️ Architecture

```
ClipForge.sln
├── ClipForge.Core/                 # Shared Business Logic & Infrastructure
│   ├── Interfaces/                 # ICaptureService, IHotkeyService, ITrayService, etc.
│   ├── Models/                     # ClipItem, AppSettings, ToastMessage
│   └── Services/                   # FfmpegService, ThumbnailService, ClipLibraryService, etc.
├── ClipForge.UI/                   # Avalonia XAML UI
│   ├── Assets/                     # Vector icons, App Icon (ICO/PNG)
│   ├── Themes/                     # Dark theme (#101112) with warm gold (#E5B84A)
│   └── Views/                      # MainWindow, PlayerWindow, Toasts, Dialogs
├── ClipForge.Platform.Windows/     # Windows Desktop Target (win-x64)
│   ├── WindowsCaptureService.cs    # Screen capture via GDI / Desktop Duplication
│   ├── WindowsHotkeyService.cs     # Win32 RegisterHotKey integration
│   └── WindowsTrayService.cs       # Windows notification area tray
├── ClipForge.Platform.Linux/       # Linux Desktop Target (linux-x64)
│   ├── LinuxCaptureService.cs      # Screen capture via x11grab
│   ├── LinuxHotkeyService.cs       # Linux global hotkeys
│   ├── LinuxTrayService.cs         # Desktop notifications & system tray
│   └── ClipForge.desktop           # FreeDesktop application entry
└── ClipForge.Tests/                # Automated Test Suite (XUnit)
```

---

## 🚀 Building & Publishing

### Linux (linux-x64)
Requires: .NET 8 SDK & FFmpeg
```bash
./build-linux.sh
# Or publish self-contained binary:
./publish-linux.sh
```
Output: `publish/linux-x64/ClipForge`

### Windows (win-x64)
Requires: .NET 8 SDK & FFmpeg
```cmd
build-windows.bat
:: Or publish self-contained binary:
publish-windows.bat
```
Output: `publish\win-x64\ClipForge.exe`
