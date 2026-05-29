using TorrentCs.Data;

namespace TorrentCs.Tests.Data;

public class MetainfoTests
{
    [Fact]
    public void Properties_AreSetCorrectly()
    {
        var meta = BuildMetainfo();
        Assert.Equal("test", meta.Name);
        Assert.Equal(3, meta.Files.Count);
        Assert.Equal(2, meta.Pieces.Count);
        Assert.Equal(256, meta.PieceSize);
        Assert.Single(meta.Trackers);
    }

    [Fact]
    public void PieceOffset_ReturnsIndexTimesSize()
    {
        var meta = BuildMetainfo();
        Assert.Equal(0L, meta.PieceOffset(meta.Pieces[0]));
        Assert.Equal(256L, meta.PieceOffset(meta.Pieces[1]));
    }

    [Fact]
    public void FileIndex_ReturnsCorrectIndex()
    {
        var meta = BuildMetainfo();
        // File 0: bytes 0-99, File 1: bytes 100-299, File 2: bytes 300-349
        Assert.Equal(0, meta.FileIndex(0));
        Assert.Equal(0, meta.FileIndex(99));
        Assert.Equal(1, meta.FileIndex(100));
        Assert.Equal(1, meta.FileIndex(299));
        Assert.Equal(2, meta.FileIndex(300));
    }

    [Fact]
    public void FileOffset_ReturnsCorrectOffset()
    {
        var meta = BuildMetainfo();
        Assert.Equal(0L, meta.FileOffset(0));
        Assert.Equal(100L, meta.FileOffset(1));
        Assert.Equal(300L, meta.FileOffset(2));
    }

    private static Metainfo BuildMetainfo()
    {
        var files = new[]
        {
            new ContainedFile("a.txt", 100),
            new ContainedFile("b.txt", 200),
            new ContainedFile("c.txt", 50),
        };
        var pieces = new[]
        {
            new Piece(0, 256, Sha1Hash.Empty),
            new Piece(1, 94, Sha1Hash.Empty),
        };
        return new Metainfo("test", Sha1Hash.Empty, files, pieces, 256, ["udp://tracker.example.com"], []);
    }
}
