using TorrentCs.Data;

namespace TorrentCs.Tests.Data;

public class Sha1HashTests
{
    [Fact]
    public void Constructor_ThrowsForWrongLength()
    {
        Assert.Throws<ArgumentException>(() => new Sha1Hash(new byte[19]));
        Assert.Throws<ArgumentException>(() => new Sha1Hash(new byte[21]));
    }

    [Fact]
    public void Constructor_AcceptsExactlyTwentyBytes()
    {
        var hash = new Sha1Hash(new byte[20]);
        Assert.Equal(20, hash.Value.Length);
    }

    [Fact]
    public void Empty_IsAllZeros()
    {
        Assert.All(Sha1Hash.Empty.Value, b => Assert.Equal(0, b));
    }

    [Fact]
    public void EqualityOperator_ReturnsTrueForSameBytes()
    {
        var bytes = new byte[20];
        bytes[0] = 1;
        var a = new Sha1Hash(bytes);
        var b = new Sha1Hash((byte[])bytes.Clone());
        Assert.True(a == b);
    }

    [Fact]
    public void EqualityOperator_ReturnsFalseForDifferentBytes()
    {
        var a = new Sha1Hash(new byte[20]);
        var bytes = new byte[20]; bytes[0] = 1;
        var b = new Sha1Hash(bytes);
        Assert.False(a == b);
    }

    [Fact]
    public void InequalityOperator_Works()
    {
        var a = new Sha1Hash(new byte[20]);
        var bytes = new byte[20]; bytes[1] = 5;
        var b = new Sha1Hash(bytes);
        Assert.True(a != b);
    }

    [Fact]
    public void Equals_NullReturnsFalse()
    {
        var hash = new Sha1Hash(new byte[20]);
        Assert.False(hash.Equals(null));
    }

    [Fact]
    public void ImplicitConversion_ToBytesReturnsValue()
    {
        var bytes = new byte[20]; bytes[5] = 42;
        var hash = new Sha1Hash(bytes);
        byte[] converted = hash;
        Assert.Equal(bytes, converted);
    }

    [Fact]
    public void GetHashCode_EqualHashesHaveSameCode()
    {
        var bytes = new byte[20]; bytes[0] = 7;
        var a = new Sha1Hash(bytes);
        var b = new Sha1Hash((byte[])bytes.Clone());
        Assert.Equal(a.GetHashCode(), b.GetHashCode());
    }

    [Fact]
    public void ToString_ReturnsEightCharacters()
    {
        var hash = new Sha1Hash(new byte[20]);
        Assert.Equal(8, hash.ToString().Length);
    }

    [Fact]
    public void OperatorEquality_BothNull_ReturnsTrue()
    {
        Sha1Hash? a = null, b = null;
        Assert.True(a == b);
    }

    [Fact]
    public void OperatorEquality_OneNull_ReturnsFalse()
    {
        Sha1Hash? a = new Sha1Hash(new byte[20]);
        Sha1Hash? b = null;
        Assert.False(a == b);
        Assert.False(b == a);
    }
}
