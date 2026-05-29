namespace TorrentCs.Data;

public interface IFileHandler : IDisposable
{
    Stream GetFileStream(string fileName);
    void CloseFileStream(Stream file);
    void Flush();
}
