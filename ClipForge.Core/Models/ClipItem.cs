using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ClipForge.Core.Models;

public class ClipItem : INotifyPropertyChanged
{
    private string _filePath = string.Empty;
    private string _fileName = string.Empty;
    private TimeSpan _duration = TimeSpan.Zero;
    private DateTime _createdAt = DateTime.MinValue;
    private long _fileSize = 0;
    private string? _thumbnailPath;
    private bool _isLoadingThumbnail;
    private string _resolution = "1080p";
    private int _fps = 60;

    public event PropertyChangedEventHandler? PropertyChanged;

    public string FilePath
    {
        get => _filePath;
        set
        {
            if (_filePath != value)
            {
                _filePath = value;
                OnPropertyChanged();
                FileName = Path.GetFileName(value);
            }
        }
    }

    public string FileName
    {
        get => _fileName;
        set
        {
            if (_fileName != value)
            {
                _fileName = value;
                OnPropertyChanged();
            }
        }
    }

    public TimeSpan Duration
    {
        get => _duration;
        set
        {
            if (_duration != value)
            {
                _duration = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(DurationFormatted));
            }
        }
    }

    public string DurationFormatted
    {
        get
        {
            if (Duration.TotalHours >= 1)
                return Duration.ToString(@"h\:mm\:ss");
            return Duration.ToString(@"m\:ss");
        }
    }

    public DateTime CreatedAt
    {
        get => _createdAt;
        set
        {
            if (_createdAt != value)
            {
                _createdAt = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(TimestampFormatted));
            }
        }
    }

    public string TimestampFormatted
    {
        get
        {
            var now = DateTime.Now;
            if (CreatedAt.Date == now.Date)
                return $"Today {CreatedAt:HH:mm}";
            if (CreatedAt.Date == now.Date.AddDays(-1))
                return $"Yesterday {CreatedAt:HH:mm}";
            return CreatedAt.ToString("MMM d, yyyy HH:mm");
        }
    }

    public long FileSize
    {
        get => _fileSize;
        set
        {
            if (_fileSize != value)
            {
                _fileSize = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(FileSizeFormatted));
            }
        }
    }

    public string FileSizeFormatted
    {
        get
        {
            double mb = FileSize / (1024.0 * 1024.0);
            if (mb >= 1024.0)
                return $"{mb / 1024.0:F2} GB";
            return $"{mb:F1} MB";
        }
    }

    public string? ThumbnailPath
    {
        get => _thumbnailPath;
        set
        {
            if (_thumbnailPath != value)
            {
                _thumbnailPath = value;
                OnPropertyChanged();
            }
        }
    }

    public bool IsLoadingThumbnail
    {
        get => _isLoadingThumbnail;
        set
        {
            if (_isLoadingThumbnail != value)
            {
                _isLoadingThumbnail = value;
                OnPropertyChanged();
            }
        }
    }

    public string Resolution
    {
        get => _resolution;
        set
        {
            if (_resolution != value)
            {
                _resolution = value;
                OnPropertyChanged();
            }
        }
    }

    public int Fps
    {
        get => _fps;
        set
        {
            if (_fps != value)
            {
                _fps = value;
                OnPropertyChanged();
            }
        }
    }

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
