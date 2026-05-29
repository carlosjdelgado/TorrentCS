using TorrentCs.Data;

namespace TorrentCs.Tests.Data;

public class DiskFileHandlerTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public DiskFileHandlerTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { /* best-effort */ }
    }

    [Fact]
    public void GetFileStream_CreatesFileIfNotExists()
    {
        using var handler = new DiskFileHandler(_tempDir);
        var stream = handler.GetFileStream("new.dat");

        Assert.NotNull(stream);
        Assert.True(File.Exists(Path.Combine(_tempDir, "new.dat")));
    }

    [Fact]
    public void GetFileStream_SameFileName_ReturnsSameStream()
    {
        using var handler = new DiskFileHandler(_tempDir);
        var s1 = handler.GetFileStream("same.dat");
        var s2 = handler.GetFileStream("same.dat");
        Assert.Same(s1, s2);
    }

    [Fact]
    public void GetFileStream_CreatesSubdirectoryIfNeeded()
    {
        using var handler = new DiskFileHandler(_tempDir);
        handler.GetFileStream(Path.Combine("sub", "file.dat"));
        Assert.True(Directory.Exists(Path.Combine(_tempDir, "sub")));
    }

    [Fact]
    public void CloseFileStream_RemovesFromTracking()
    {
        var handler = new DiskFileHandler(_tempDir);
        var stream = handler.GetFileStream("close.dat");
        handler.CloseFileStream(stream);
        // After close, requesting again should open a new stream
        var newStream = handler.GetFileStream("close.dat");
        Assert.NotSame(stream, newStream);
        handler.Dispose();
    }

    [Fact]
    public void CloseFileStream_UntrackedStream_Throws()
    {
        using var handler = new DiskFileHandler(_tempDir);
        using var alien = new MemoryStream();
        Assert.Throws<InvalidOperationException>(() => handler.CloseFileStream(alien));
    }

    [Fact]
    public void Flush_DoesNotThrow()
    {
        using var handler = new DiskFileHandler(_tempDir);
        handler.GetFileStream("flush.dat");
        handler.Flush();
    }

    [Fact]
    public void Directory_Property_ReturnsConstructorValue()
    {
        using var handler = new DiskFileHandler(_tempDir);
        Assert.Equal(_tempDir, handler.Directory);
    }
}
