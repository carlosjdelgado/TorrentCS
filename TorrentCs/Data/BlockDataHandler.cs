namespace TorrentCs.Data;

public class BlockDataHandler : IBlockDataHandler
{
    private readonly IFileHandler _fileHandler;
    // Serialises seek+read/write against the shared file streams, which several peer threads use.
    private readonly object _lock = new();

    public BlockDataHandler(IFileHandler fileHandler, Metainfo metainfo)
    {
        _fileHandler = fileHandler;
        Metainfo = metainfo;
    }

    public Metainfo Metainfo { get; }

    public byte[] ReadBlockData(long offset, int length)
    {
        var data = new byte[length];
        lock (_lock) ReadInto(offset, data);
        return data;
    }

    public bool TryReadBlockData(long offset, int length, out byte[] data)
    {
        data = new byte[length];
        lock (_lock) return ReadInto(offset, data);
    }

    public void WriteBlockData(long offset, byte[] data)
    {
        lock (_lock)
        {
            long position = 0;
            int written = 0;

            foreach (var file in Metainfo.Files)
            {
                long fileEnd = position + file.Size;

                if (written < data.Length && offset < fileEnd && offset + data.Length > position)
                {
                    long fileOffset = Math.Max(0, offset - position);
                    int dataOffset = (int)Math.Max(0, position - offset);
                    int count = (int)Math.Min(file.Size - fileOffset, data.Length - dataOffset);

                    var stream = _fileHandler.GetFileStream(file.Name);
                    if (stream.Length < fileOffset)
                        stream.SetLength(fileOffset);
                    stream.Seek(fileOffset, SeekOrigin.Begin);
                    stream.Write(data, dataOffset, count);
                    written += count;
                }

                position += file.Size;
                if (written >= data.Length) break;
            }
        }
    }

    public void Flush() => _fileHandler.Flush();

    private bool ReadInto(long offset, byte[] buffer)
    {
        long position = 0;
        int totalRead = 0;

        foreach (var file in Metainfo.Files)
        {
            long fileEnd = position + file.Size;

            if (totalRead < buffer.Length && offset < fileEnd && offset + buffer.Length > position)
            {
                long fileOffset = Math.Max(0, offset - position);
                int bufferOffset = (int)Math.Max(0, position - offset);
                int count = (int)Math.Min(file.Size - fileOffset, buffer.Length - bufferOffset);

                var stream = _fileHandler.GetFileStream(file.Name);
                if (stream.Length < fileOffset + count)
                    return false;
                stream.Seek(fileOffset, SeekOrigin.Begin);
                int bytesRead = stream.Read(buffer, bufferOffset, count);
                if (bytesRead < count)
                    return false;
                totalRead += bytesRead;
            }

            position += file.Size;
            if (totalRead >= buffer.Length) break;
        }

        return totalRead == buffer.Length;
    }
}
