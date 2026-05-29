using TorrentCs.Application.BitTorrent;

namespace TorrentCs.Tests.Application.BitTorrent;

public class BlockRequestTests
{
    [Fact]
    public void Properties_AreSetCorrectly()
    {
        var req = new BlockRequest(2, 16384, 16384);
        Assert.Equal(2, req.PieceIndex);
        Assert.Equal(16384, req.Offset);
        Assert.Equal(16384, req.Length);
    }

    [Fact]
    public void Equality_SameValues_AreEqual()
    {
        var a = new BlockRequest(1, 0, 100);
        var b = new BlockRequest(1, 0, 100);
        Assert.True(a == b);
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equality_DifferentValues_NotEqual()
    {
        var a = new BlockRequest(1, 0, 100);
        var b = new BlockRequest(1, 1, 100);
        Assert.True(a != b);
    }

    [Fact]
    public void GetHashCode_EqualObjectsSameCode()
    {
        var a = new BlockRequest(3, 512, 256);
        var b = new BlockRequest(3, 512, 256);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
