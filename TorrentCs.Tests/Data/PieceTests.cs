using TorrentCs.Data;

namespace TorrentCs.Tests.Data;

public class PieceTests
{
    [Fact]
    public void Constructor_IndexOnly_SetsIndex()
    {
        var piece = new Piece(5);
        Assert.Equal(5, piece.Index);
        Assert.Equal(0, piece.Size);
        Assert.Null(piece.Hash);
    }

    [Fact]
    public void Constructor_Full_SetsAllProperties()
    {
        var hash = new Sha1Hash(new byte[20]);
        var piece = new Piece(3, 262144, hash);

        Assert.Equal(3, piece.Index);
        Assert.Equal(262144, piece.Size);
        Assert.Same(hash, piece.Hash);
    }
}
