using System.Collections.ObjectModel;
using ClipForge.Core.Models;

namespace ClipForge.Core.Services;

public enum ClipSortOrder
{
    DateDesc,
    DateAsc,
    NameAsc,
    NameDesc,
    DurationDesc,
    SizeDesc
}

public class ClipLibraryService
{
    private readonly FfmpegService _ffmpegService;
    private readonly ThumbnailService _thumbnailService;
    private readonly DiagnosticsService _diagnostics;
    private readonly Action<Action> _dispatchToUi;

    private CancellationTokenSource? _scanCts;
    private readonly List<ClipItem> _allClips = new();
    private readonly ObservableCollection<ClipItem> _visibleClips = new();
    private readonly ObservableCollection<ClipItem> _recentClips = new();

    private string _searchQuery = string.Empty;
    private ClipSortOrder _sortOrder = ClipSortOrder.DateDesc;
    private string? _durationFilter = null;

    public ObservableCollection<ClipItem> VisibleClips => _visibleClips;
    public ObservableCollection<ClipItem> RecentClips => _recentClips;
    public bool IsScanning { get; private set; }

    public event Action? LibraryChanged;

    public ClipLibraryService(
        FfmpegService ffmpegService,
        ThumbnailService thumbnailService,
        DiagnosticsService diagnostics,
        Action<Action> dispatchToUi)
    {
        _ffmpegService = ffmpegService;
        _thumbnailService = thumbnailService;
        _diagnostics = diagnostics;
        _dispatchToUi = dispatchToUi;
    }

    public async Task ScanLibraryAsync(string clipsFolder)
    {
        _scanCts?.Cancel();
        _scanCts = new CancellationTokenSource();
        var ct = _scanCts.Token;

        IsScanning = true;
        try
        {
            if (!Directory.Exists(clipsFolder))
                Directory.CreateDirectory(clipsFolder);

            var videoExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".mp4", ".mkv", ".mov", ".webm", ".avi", ".ts"
            };

            var items = await Task.Run(() =>
            {
                var di = new DirectoryInfo(clipsFolder);
                return di.EnumerateFiles()
                         .Where(f => videoExtensions.Contains(f.Extension))
                         .OrderByDescending(f => f.LastWriteTimeUtc)
                         .Select(fi => new ClipItem
                         {
                             FilePath = fi.FullName,
                             FileName = fi.Name,
                             CreatedAt = fi.LastWriteTime,
                             FileSize = fi.Length,
                             Duration = TimeSpan.FromSeconds(30),
                             IsLoadingThumbnail = true
                         })
                         .ToList();
            }, ct);

            if (ct.IsCancellationRequested) return;

            _dispatchToUi(() =>
            {
                _allClips.Clear();
                _allClips.AddRange(items);
                ApplyFilterAndSortInternal();
                UpdateRecentClipsInternal();
            });

            _ = Task.Run(async () =>
            {
                foreach (var item in items)
                {
                    if (ct.IsCancellationRequested) break;
                    try
                    {
                        var thumbPath = await _thumbnailService.GetThumbnailPathAsync(item.FilePath, ct);
                        _dispatchToUi(() =>
                        {
                            item.ThumbnailPath = thumbPath;
                            item.IsLoadingThumbnail = false;
                        });

                        var meta = await _ffmpegService.GetVideoMetadataAsync(item.FilePath, ct);
                        if (meta.Duration > TimeSpan.Zero)
                        {
                            _dispatchToUi(() =>
                            {
                                item.Duration = meta.Duration;
                                item.Resolution = $"{meta.Width}x{meta.Height}";
                                item.Fps = meta.Fps;
                            });
                        }
                    }
                    catch { }
                }
            }, ct);
        }
        catch (Exception ex)
        {
            _diagnostics.LogError($"Scan library failed for {clipsFolder}", ex);
        }
        finally
        {
            IsScanning = false;
        }
    }

    public void SetSearchQuery(string query)
    {
        _searchQuery = query?.Trim() ?? string.Empty;
        ApplyFilterAndSortInternal();
    }

    public void SetSortOrder(ClipSortOrder sortOrder)
    {
        _sortOrder = sortOrder;
        ApplyFilterAndSortInternal();
    }

    public void SetDurationFilter(string? filter)
    {
        _durationFilter = filter;
        ApplyFilterAndSortInternal();
    }

    private void ApplyFilterAndSortInternal()
    {
        var filtered = _allClips.AsEnumerable();

        if (!string.IsNullOrEmpty(_searchQuery))
            filtered = filtered.Where(c => c.FileName.Contains(_searchQuery, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrEmpty(_durationFilter))
        {
            filtered = _durationFilter switch
            {
                "<30s" => filtered.Where(c => c.Duration.TotalSeconds < 30),
                "30s-60s" => filtered.Where(c => c.Duration.TotalSeconds >= 30 && c.Duration.TotalSeconds <= 60),
                ">60s" => filtered.Where(c => c.Duration.TotalSeconds > 60),
                _ => filtered
            };
        }

        filtered = _sortOrder switch
        {
            ClipSortOrder.DateAsc => filtered.OrderBy(c => c.CreatedAt),
            ClipSortOrder.NameAsc => filtered.OrderBy(c => c.FileName, StringComparer.OrdinalIgnoreCase),
            ClipSortOrder.NameDesc => filtered.OrderByDescending(c => c.FileName, StringComparer.OrdinalIgnoreCase),
            ClipSortOrder.DurationDesc => filtered.OrderByDescending(c => c.Duration),
            ClipSortOrder.SizeDesc => filtered.OrderByDescending(c => c.FileSize),
            _ => filtered.OrderByDescending(c => c.CreatedAt)
        };

        _visibleClips.Clear();
        foreach (var c in filtered) _visibleClips.Add(c);
        LibraryChanged?.Invoke();
    }

    private void UpdateRecentClipsInternal()
    {
        _recentClips.Clear();
        foreach (var c in _allClips.OrderByDescending(x => x.CreatedAt).Take(4))
            _recentClips.Add(c);
    }

    public async Task<(bool Success, string ErrorMessage, string NewPath)> RenameClipAsync(ClipItem clip, string newBaseName)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newBaseName)) return (false, "Filename cannot be empty.", string.Empty);
                var invalid = Path.GetInvalidFileNameChars();
                if (newBaseName.IndexOfAny(invalid) >= 0) return (false, "Filename contains invalid characters.", string.Empty);

                var dir = Path.GetDirectoryName(clip.FilePath);
                if (dir == null) return (false, "Invalid directory.", string.Empty);

                var ext = Path.GetExtension(clip.FilePath);
                var targetName = newBaseName + ext;
                var targetPath = Path.Combine(dir, targetName);

                if (File.Exists(targetPath) && !string.Equals(clip.FilePath, targetPath, StringComparison.OrdinalIgnoreCase))
                    return (false, "A clip with this name already exists.", string.Empty);

                File.Move(clip.FilePath, targetPath);

                _dispatchToUi(() =>
                {
                    clip.FilePath = targetPath;
                    clip.FileName = targetName;
                });
                return (true, string.Empty, targetPath);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, string.Empty);
            }
        });
    }

    public async Task<(bool Success, string ErrorMessage)> DeleteClipAsync(ClipItem clip)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (File.Exists(clip.FilePath)) File.Delete(clip.FilePath);
                _dispatchToUi(() =>
                {
                    _allClips.Remove(clip);
                    _visibleClips.Remove(clip);
                    _recentClips.Remove(clip);
                    LibraryChanged?.Invoke();
                });
                return (true, string.Empty);
            }
            catch (Exception ex)
            {
                return (false, ex.Message);
            }
        });
    }

    public async Task<(bool Success, string ErrorMessage, string NewPath)> MoveClipAsync(ClipItem clip, string targetDirectory)
    {
        return await Task.Run(() =>
        {
            try
            {
                if (!Directory.Exists(targetDirectory)) Directory.CreateDirectory(targetDirectory);
                var targetPath = Path.Combine(targetDirectory, clip.FileName);
                if (File.Exists(targetPath))
                {
                    targetPath = Path.Combine(targetDirectory, $"{Path.GetFileNameWithoutExtension(clip.FileName)}_{DateTime.Now:yyyyMMddHHmmss}{Path.GetExtension(clip.FileName)}");
                }
                File.Move(clip.FilePath, targetPath);
                _dispatchToUi(() =>
                {
                    clip.FilePath = targetPath;
                    LibraryChanged?.Invoke();
                });
                return (true, string.Empty, targetPath);
            }
            catch (Exception ex)
            {
                return (false, ex.Message, string.Empty);
            }
        });
    }
}
