using TorrentCs.Application.BitTorrent;

namespace TorrentCs.Tests.Application.BitTorrent;

public class BitfieldTests
{
    [Fact]
    public void PieceCount_MatchesConstructor()
    {
        Assert.Equal(50, new Bitfield(50).PieceCount);
    }

    [Fact]
    public void InitiallyAllFalse()
    {
        var bf = new Bitfield(10);
        for (int i = 0; i < 10; i++)
            Assert.False(bf[i]);
    }

    [Fact]
    public void SetPieceAvailable_True_CanBeRead()
    {
        var bf = new Bitfield(8);
        bf.SetPieceAvailable(3, true);
        Assert.True(bf.IsPieceAvailable(3));
        Assert.False(bf.IsPieceAvailable(2));
    }

    [Fact]
    public void SetPieceAvailable_False_ClearsIt()
    {
        var bf = new Bitfield(8);
        bf[5] = true;
        bf[5] = false;
        Assert.False(bf[5]);
    }

    [Fact]
    public void HasPiece_ReturnsTrueWhenSet()
    {
        var bf = new Bitfield(5);
        bf[3] = true;
        Assert.True(bf.HasPiece(3));
        Assert.False(bf.HasPiece(0));
    }

    [Fact]
    public void GetAvailablePiecesCount_AllFalse_ReturnsZero()
    {
        Assert.Equal(0, new Bitfield(10).GetAvailablePiecesCount());
    }

    [Fact]
    public void GetAvailablePiecesCount_AllTrue_ReturnsPieceCount()
    {
        var bf = new Bitfield(10);
        bf.SetAll(true);
        Assert.Equal(10, bf.GetAvailablePiecesCount());
    }

    [Fact]
    public void RemainingPiecesCount_IsComplement()
    {
        var bf = new Bitfield(10);
        bf[0] = true;
        bf[1] = true;
        Assert.Equal(8, bf.RemainingPiecesCount());
    }

    [Fact]
    public void SetAll_True_SetsAllPieces()
    {
        var bf = new Bitfield(8);
        bf.SetAll(true);
        for (int i = 0; i < 8; i++)
            Assert.True(bf[i]);
    }

    [Fact]
    public void SetAll_False_AfterTrue_AllFalse()
    {
        var bf = new Bitfield(4);
        bf.SetAll(true);
        bf.SetAll(false);
        for (int i = 0; i < 4; i++)
            Assert.False(bf[i]);
    }

    [Fact]
    public void Union_CombinesBits()
    {
        var a = new Bitfield(8); a[0] = true; a[2] = true;
        var b = new Bitfield(8); b[1] = true; b[2] = true;
        a.Union(b);
        Assert.True(a[0]);
        Assert.True(a[1]);
        Assert.True(a[2]);
    }

    [Fact]
    public void NotSubset_ReturnsTrueWhenThisHasExtraPieces()
    {
        var a = new Bitfield(4); a[0] = true;
        var b = new Bitfield(4);
        Assert.True(a.NotSubset(b));
    }

    [Fact]
    public void NotSubset_ReturnsFalseWhenSubset()
    {
        var a = new Bitfield(4); a[0] = true;
        var b = new Bitfield(4); b[0] = true; b[1] = true;
        Assert.False(a.NotSubset(b));
    }

    [Fact]
    public void ToBytes_RoundtripFromBytes()
    {
        var original = new Bitfield(16);
        original[0] = true;
        original[7] = true;
        original[15] = true;

        var restored = new Bitfield(16, original.ToBytes());
        Assert.True(restored[0]);
        Assert.True(restored[7]);
        Assert.True(restored[15]);
        Assert.False(restored[1]);
    }

    [Fact]
    public void MSBFirst_PieceZeroIsHighestBit()
    {
        var bytes = new byte[] { 0b10000000 };
        var bf = new Bitfield(8, bytes);
        Assert.True(bf[0]);
        Assert.False(bf[1]);
    }

    [Fact]
    public void ToString_ContainsPercentSign()
    {
        var bf = new Bitfield(10);
        bf.SetAll(true);
        Assert.Contains("%", bf.ToString());
    }
}
