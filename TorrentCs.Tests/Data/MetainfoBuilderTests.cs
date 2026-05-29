using TorrentCs.Data;

namespace TorrentCs.Tests.Data;

public class MetainfoBuilderTests
{
    [Fact]
    public void Build_SingleFile_CreatesCorrectPieceCount()
    {
        var data = new byte[1024];
        var meta = new MetainfoBuilder("test")
            .AddFile("file.bin", data)
            .WithPieceSize(512)
            .Build();

        Assert.Equal(2, meta.Pieces.Count);
    }

    [Fact]
    public void Build_LastPieceCanBeSmallerThanPieceSize()
    {
        var data = new byte[700];
        var meta = new MetainfoBuilder("test")
            .AddFile("file.bin", data)
            .WithPieceSize(512)
            .Build();

        Assert.Equal(2, meta.Pieces.Count);
        Assert.Equal(512, meta.Pieces[0].Size);
        Assert.Equal(188, meta.Pieces[1].Size);
    }

    [Fact]
    public void Build_ExactMultiple_AllPiecesFullSize()
    {
        var data = new byte[1024];
        var meta = new MetainfoBuilder("test")
            .AddFile("file.bin", data)
            .WithPieceSize(256)
            .Build();

        Assert.All(meta.Pieces, p => Assert.Equal(256, p.Size));
    }

    [Fact]
    public void Build_PiecesHaveNonNullHash()
    {
        var data = new byte[256];
        var meta = new MetainfoBuilder("test")
            .AddFile("a.bin", data)
            .WithPieceSize(256)
            .Build();

        Assert.All(meta.Pieces, p => Assert.NotNull(p.Hash));
    }

    [Fact]
    public void WithTracker_AppearsInMetainfo()
    {
        var meta = new MetainfoBuilder("test")
            .AddFile("f.bin", new byte[10])
            .WithTracker("udp://tracker.example.com:6969")
            .Build();

        Assert.Single(meta.Trackers);
        Assert.Equal("udp://tracker.example.com:6969", meta.Trackers[0]);
    }

    [Fact]
    public void Build_MultipleFiles_SpanningPieces()
    {
        var meta = new MetainfoBuilder("test")
            .AddFile("a.bin", new byte[300])
            .AddFile("b.bin", new byte[300])
            .WithPieceSize(256)
            .Build();

        // 600 bytes / 256 = 3 pieces (256, 256, 88)
        Assert.Equal(3, meta.Pieces.Count);
        Assert.Equal("test", meta.Name);
        Assert.Equal(2, meta.Files.Count);
    }

    [Fact]
    public void Build_InfoHash_IsNotEmpty()
    {
        var meta = new MetainfoBuilder("test")
            .AddFile("x.bin", new byte[] { 1, 2, 3 })
            .Build();

        Assert.NotEqual(Sha1Hash.Empty, meta.InfoHash);
    }
}
