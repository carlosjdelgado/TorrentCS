using TorrentCs.Data;

namespace TorrentCs.Tests.Data;

public class BlockTests
{
    [Fact]
    public void Properties_AreSetCorrectly()
    {
        var data = new byte[] { 1, 2, 3 };
        var block = new Block(2, 16, data);

        Assert.Equal(2, block.PieceIndex);
        Assert.Equal(16, block.Offset);
        Assert.Same(data, block.Data);
    }

    [Fact]
    public void Length_EqualsDataLength()
    {
        var block = new Block(0, 0, new byte[42]);
        Assert.Equal(42, block.Length);
    }

    [Fact]
    public void Equals_SamePieceIndexAndOffset_ReturnsTrue()
    {
        var a = new Block(1, 256, new byte[] { 1, 2 });
        var b = new Block(1, 256, new byte[] { 3, 4 });
        Assert.Equal(a, b);
    }

    [Fact]
    public void Equals_DifferentOffset_ReturnsFalse()
    {
        var a = new Block(1, 0, new byte[4]);
        var b = new Block(1, 4, new byte[4]);
        Assert.NotEqual(a, b);
    }

    [Fact]
    public void GetHashCode_SameLogicalBlock_SameCode()
    {
        var a = new Block(3, 512, new byte[8]);
        var b = new Block(3, 512, new byte[16]);
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }
}
