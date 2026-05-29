namespace TorrentCs.Data;

public class DiskFileHandler : IFileHandler
{
    private readonly Dictionary<Stream, string> _openFiles = new();

    public DiskFileHandler(string directory)
    {
        Directory = directory;
    }

    public string Directory { get; }

    public Stream GetFileStream(string fileName)
    {
        var existing = _openFiles.FirstOrDefault(x => x.Value == fileName).Key;
        if (existing is not null)
            return existing;

        var fullPath = FullName(fileName);
        var dir = Path.GetDirectoryName(fullPath)!;
        if (!System.IO.Directory.Exists(dir))
            System.IO.Directory.CreateDirectory(dir);

        var stream = new FileStream(fullPath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read);
        _openFiles[stream] = fileName;
        return stream;
    }

    public void CloseFileStream(Stream file)
    {
        if (!_openFiles.Remove(file))
            throw new InvalidOperationException("The stream is not tracked by this handler.");
        file.Dispose();
    }

    public void Flush()
    {
        foreach (var stream in _openFiles.Keys)
            stream.Flush();
    }

    public void Dispose()
    {
        foreach (var stream in _openFiles.Keys)
            stream.Dispose();
        _openFiles.Clear();
    }

    private string FullName(string fileName) => Path.Combine(Directory, fileName);
}
