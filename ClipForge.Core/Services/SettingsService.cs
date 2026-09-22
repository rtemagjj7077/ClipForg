using System.Text.Json;
using ClipForge.Core.Models;

namespace ClipForge.Core.Services;

public class SettingsService
{
    private readonly string _settingsFilePath;
    private readonly DiagnosticsService _diagnostics;
    private readonly SemaphoreSlim _fileLock = new(1, 1);
    private AppSettings _currentSettings;

    public event Action<AppSettings>? SettingsChanged;
    public AppSettings Current => _currentSettings;

    public SettingsService(DiagnosticsService diagnostics)
    {
        _diagnostics = diagnostics;
        var dataDir = StorageService.GetDefaultDataDirectory();
        try
        {
            if (!Directory.Exists(dataDir)) Directory.CreateDirectory(dataDir);
        }
        catch { }

        _settingsFilePath = Path.Combine(dataDir, "settings.json");
        _currentSettings = LoadSettingsInternal();
    }

    private AppSettings LoadSettingsInternal()
    {
        try
        {
            if (File.Exists(_settingsFilePath))
            {
                var json = File.ReadAllText(_settingsFilePath);
                var settings = JsonSerializer.Deserialize<AppSettings>(json);
                if (settings != null)
                {
                    _diagnostics.LogInfo("Settings loaded successfully from disk.");
                    return settings;
                }
            }
        }
        catch (Exception ex)
        {
            _diagnostics.LogError("Settings file corrupted or missing. Falling back to defaults.", ex);
        }

        var defaultSettings = AppSettings.CreateDefault();
        SaveSettingsFireAndForget(defaultSettings);
        return defaultSettings;
    }

    public async Task SaveSettingsAsync(AppSettings settings)
    {
        _currentSettings = settings;
        SettingsChanged?.Invoke(_currentSettings);

        await _fileLock.WaitAsync();
        try
        {
            var dir = Path.GetDirectoryName(_settingsFilePath);
            if (dir != null && !Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var tempFile = _settingsFilePath + ".tmp." + Guid.NewGuid().ToString("N");
            var json = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
            await File.WriteAllTextAsync(tempFile, json);
            File.Move(tempFile, _settingsFilePath, overwrite: true);
            _diagnostics.LogInfo("Settings saved atomically to disk.");
        }
        catch (Exception ex)
        {
            _diagnostics.LogError("Failed to save settings to disk.", ex);
        }
        finally
        {
            _fileLock.Release();
        }
    }

    public void SaveSettingsFireAndForget(AppSettings settings)
    {
        _ = Task.Run(async () => await SaveSettingsAsync(settings));
    }

    public async Task ResetToDefaultsAsync()
    {
        await SaveSettingsAsync(AppSettings.CreateDefault());
    }
}
