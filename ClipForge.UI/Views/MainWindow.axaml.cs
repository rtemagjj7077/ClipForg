using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ClipForge.Core.Models;
using ClipForge.Core.Services;

namespace ClipForge.UI.Views;

public partial class MainWindow : Window
{
    private readonly DiagnosticsService _diagnostics;
    private readonly StorageService _storageService;
    private readonly SettingsService _settingsService;
    private readonly FfmpegService _ffmpegService;
    private readonly ThumbnailService _thumbnailService;
    private readonly ClipLibraryService _clipLibraryService;

    public MainWindow()
    {
        InitializeComponent();

        _diagnostics = new DiagnosticsService();
        _storageService = new StorageService(_diagnostics);
        _settingsService = new SettingsService(_diagnostics);
        _ffmpegService = new FfmpegService(_diagnostics);
        _thumbnailService = new ThumbnailService(_ffmpegService, _storageService, _diagnostics);
        _clipLibraryService = new ClipLibraryService(_ffmpegService, _thumbnailService, _diagnostics, action => Dispatcher.UIThread.Post(action));

        DataContext = _clipLibraryService;
        _ = _clipLibraryService.ScanLibraryAsync(_settingsService.Current.ClipFolder);
    }

    private void NavHome_Click(object? sender, RoutedEventArgs e)
    {
        PageHome.IsVisible = true;
        PageClips.IsVisible = false;
        PageSettings.IsVisible = false;
    }

    private void NavClips_Click(object? sender, RoutedEventArgs e)
    {
        PageHome.IsVisible = false;
        PageClips.IsVisible = true;
        PageSettings.IsVisible = false;
    }

    private void NavSettings_Click(object? sender, RoutedEventArgs e)
    {
        PageHome.IsVisible = false;
        PageClips.IsVisible = false;
        PageSettings.IsVisible = true;
    }

    private void BtnViewAllClips_Click(object? sender, RoutedEventArgs e) => NavClips_Click(sender, e);

    private void BtnSaveClip_Click(object? sender, RoutedEventArgs e) { }
    private void BtnToggleCapture_Click(object? sender, RoutedEventArgs e) { }

    private void ClipCard_DoubleTapped(object? sender, RoutedEventArgs e)
    {
        if (sender is Control c && c.DataContext is ClipItem clip)
        {
            var player = new PlayerWindow(clip.FilePath);
            player.Show(this);
        }
    }

    private void TxtSearch_TextChanged(object? sender, TextChangedEventArgs e)
    {
        _clipLibraryService.SetSearchQuery(TxtSearch.Text ?? string.Empty);
    }

    private void BtnCols3_Click(object? sender, RoutedEventArgs e) => UniformGridClips.Columns = 3;
    private void BtnCols4_Click(object? sender, RoutedEventArgs e) => UniformGridClips.Columns = 4;
    private void BtnRefresh_Click(object? sender, RoutedEventArgs e) => _ = _clipLibraryService.ScanLibraryAsync(_settingsService.Current.ClipFolder);

    private void MenuOpenClipForge_Click(object? sender, RoutedEventArgs e) { }
    private void MenuOpenSystem_Click(object? sender, RoutedEventArgs e) { }
    private void MenuRename_Click(object? sender, RoutedEventArgs e) { }
    private void MenuMove_Click(object? sender, RoutedEventArgs e) { }
    private void MenuShowInFolder_Click(object? sender, RoutedEventArgs e) { }
    private void MenuDelete_Click(object? sender, RoutedEventArgs e) { }

    private void BtnFps_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button b && int.TryParse(b.Tag?.ToString(), out var fps))
        {
            _settingsService.Current.Fps = fps;
            _settingsService.SaveSettingsFireAndForget(_settingsService.Current);
        }
    }

    private void BtnDur_Click(object? sender, RoutedEventArgs e)
    {
        if (sender is Button b && int.TryParse(b.Tag?.ToString(), out var dur))
        {
            _settingsService.Current.ClipDurationSeconds = dur;
            _settingsService.SaveSettingsFireAndForget(_settingsService.Current);
        }
    }
}
