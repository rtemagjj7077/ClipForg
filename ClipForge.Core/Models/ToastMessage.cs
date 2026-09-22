namespace ClipForge.Core.Models;

public enum ToastType
{
    Success,
    Info,
    Warning,
    Error
}

public class ToastMessage
{
    public Guid Id { get; } = Guid.NewGuid();
    public string Title { get; set; }
    public string Message { get; set; }
    public ToastType Type { get; set; }
    public string IconKey { get; set; }
    public int DurationMs { get; set; }

    public ToastMessage(string message, ToastType type = ToastType.Info, string iconKey = "Icon.Check", int durationMs = 3000)
    {
        Message = message;
        Type = type;
        IconKey = iconKey;
        DurationMs = durationMs;
        Title = type switch
        {
            ToastType.Success => "Success",
            ToastType.Error => "Error",
            ToastType.Warning => "Notice",
            _ => "ClipForge"
        };
    }
}
