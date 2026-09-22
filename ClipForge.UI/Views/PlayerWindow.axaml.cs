using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ClipForge.UI.Views;

public partial class PlayerWindow : Window
{
    private readonly string _filePath;

    public PlayerWindow()
        : this(string.Empty)
    {
    }

    public PlayerWindow(string filePath)
    {
        InitializeComponent();
        _filePath = filePath ?? string.Empty;
        TxtFilePath.Text = string.IsNullOrWhiteSpace(_filePath)
            ? string.Empty
            : Path.GetFileName(_filePath);
    }

    private void BtnOpenSystem_Click(object? sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_filePath)) return;

        try
        {
            Process.Start(new ProcessStartInfo { FileName = _filePath, UseShellExecute = true });
        }
        catch { }
    }

    private void BtnPlayPause_Click(object? sender, RoutedEventArgs e) { }
}
