using TorrentCs.Data;

namespace TorrentCs.Tests.Data;

public class MemoryFileHandlerTests
{
    [Fact]
    public void GetFileStream_NewFile_ReturnsEmptyStream()
    {
        using var handler = new MemoryFileHandler();
        var stream = handler.GetFileStream("a.txt");
        Assert.Equal(0, stream.Length);
    }

    [Fact]
    public void GetFileStream_SameFile_ReturnsSameStream()
    {
        using var handler = new MemoryFileHandler();
        var s1 = handler.GetFileStream("a.txt");
        var s2 = handler.GetFileStream("a.txt");
        Assert.Same(s1, s2);
    }

    [Fact]
    public void GetFileStream_DifferentFiles_ReturnDifferentStreams()
    {
        using var handler = new MemoryFileHandler();
        var s1 = handler.GetFileStream("a.txt");
        var s2 = handler.GetFileStream("b.txt");
        Assert.NotSame(s1, s2);
    }

    [Fact]
    public void Constructor_WithByteDictionary_PreloadsData()
    {
        var data = new byte[] { 10, 20, 30 };
        using var handler = new MemoryFileHandler(new Dictionary<string, byte[]> { ["f.dat"] = data });

        var stream = handler.GetFileStream("f.dat");
        var read = new byte[3];
        stream.Seek(0, SeekOrigin.Begin);
        stream.ReadExactly(read, 0, 3);
        Assert.Equal(data, read);
    }

    [Fact]
    public void Constructor_SingleFile_PreloadsData()
    {
        var data = new byte[] { 1, 2, 3, 4 };
        using var handler = new MemoryFileHandler("file.bin", data);

        var stream = handler.GetFileStream("file.bin");
        Assert.Equal(4, stream.Length);
    }

    [Fact]
    public void CloseFileStream_IsNoOp_DataRemainsAccessible()
    {
        using var handler = new MemoryFileHandler();
        var stream = handler.GetFileStream("x.bin");
        handler.CloseFileStream(stream);
        // Should still be accessible after no-op close
        var same = handler.GetFileStream("x.bin");
        Assert.NotNull(same);
    }

    [Fact]
    public void Flush_DoesNotThrow()
    {
        using var handler = new MemoryFileHandler();
        handler.GetFileStream("a.txt");
        handler.Flush();
    }
}
