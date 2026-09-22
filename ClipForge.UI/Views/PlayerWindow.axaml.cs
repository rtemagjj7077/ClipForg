using System.Diagnostics;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace ClipForge.UI.Views;

public partial class PlayerWindow : Window
{
    private readonly string _filePath;

    public PlayerWindow(string filePath)
    {
        InitializeComponent();
        _filePath = filePath;
        TxtFilePath.Text = Path.GetFileName(filePath);
    }

    private void BtnOpenSystem_Click(object? sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo { FileName = _filePath, UseShellExecute = true });
        }
        catch { }
    }

    private void BtnPlayPause_Click(object? sender, RoutedEventArgs e) { }
}
