namespace TorrentCs.Data;

public class MemoryFileHandler : IFileHandler
{
    private readonly Dictionary<string, MemoryStream> _files = new();

    public MemoryFileHandler() { }

    public MemoryFileHandler(IDictionary<string, byte[]> files)
    {
        foreach (var (name, data) in files)
        {
            var stream = new MemoryStream();
            stream.Write(data, 0, data.Length);
            stream.Seek(0, SeekOrigin.Begin);
            _files[name] = stream;
        }
    }

    public MemoryFileHandler(string fileName, byte[] data)
    {
        var stream = new MemoryStream();
        stream.Write(data, 0, data.Length);
        stream.Seek(0, SeekOrigin.Begin);
        _files[fileName] = stream;
    }

    public Stream GetFileStream(string fileName)
    {
        if (!_files.TryGetValue(fileName, out var stream))
        {
            stream = new MemoryStream();
            _files[fileName] = stream;
        }
        return stream;
    }

    // Intentionally no-op: keeps data alive in memory for the session lifetime.
    public void CloseFileStream(Stream file) { }

    public void Flush() { }

    public void Dispose()
    {
        foreach (var stream in _files.Values)
            stream.Dispose();
        _files.Clear();
    }
}
