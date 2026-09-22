namespace ClipForge.Core.Interfaces;

public interface IMediaPlayerService
{
    Task<bool> VerifyMediaPlayableAsync(string filePath);
    void OpenWithSystemDefault(string filePath);
    void ShowInFileBrowser(string filePath);
}
