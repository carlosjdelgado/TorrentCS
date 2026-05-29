using TorrentCs.Transport;

namespace TorrentCs.Tests.Transport;

public class BigEndianBinaryReaderTests
{
    private static BigEndianBinaryReader Make(params byte[] bytes)
        => new(new MemoryStream(bytes));

    [Fact]
    public void ReadInt16_BigEndianBytes_ReturnsCorrectValue()
    {
        // 0x1234 big-endian → bytes [0x12, 0x34]
        var reader = Make(0x12, 0x34);
        Assert.Equal((short)0x1234, reader.ReadInt16());
    }

    [Fact]
    public void ReadInt16_NegativeValue()
    {
        // -1 = 0xFFFF big-endian → [0xFF, 0xFF]
        var reader = Make(0xFF, 0xFF);
        Assert.Equal((short)-1, reader.ReadInt16());
    }

    [Fact]
    public void ReadUInt16_BigEndianBytes_ReturnsCorrectValue()
    {
        // 6969 = 0x1B39 → [0x1B, 0x39]
        var reader = Make(0x1B, 0x39);
        Assert.Equal((ushort)6969, reader.ReadUInt16());
    }

    [Fact]
    public void ReadUInt16_MaxValue()
    {
        var reader = Make(0xFF, 0xFF);
        Assert.Equal(ushort.MaxValue, reader.ReadUInt16());
    }

    [Fact]
    public void ReadInt32_BigEndianBytes_ReturnsCorrectValue()
    {
        // 0x01020304 → [0x01, 0x02, 0x03, 0x04]
        var reader = Make(0x01, 0x02, 0x03, 0x04);
        Assert.Equal(0x01020304, reader.ReadInt32());
    }

    [Fact]
    public void ReadInt32_NegativeValue()
    {
        // -1 = 0xFFFFFFFF → [0xFF, 0xFF, 0xFF, 0xFF]
        var reader = Make(0xFF, 0xFF, 0xFF, 0xFF);
        Assert.Equal(-1, reader.ReadInt32());
    }

    [Fact]
    public void ReadInt64_BigEndianBytes_ReturnsCorrectValue()
    {
        // 0x0102030405060708 → [0x01..0x08]
        var reader = Make(0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08);
        Assert.Equal(0x0102030405060708L, reader.ReadInt64());
    }

    [Fact]
    public void ReadInt64_NegativeOne()
    {
        var reader = Make(0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF, 0xFF);
        Assert.Equal(-1L, reader.ReadInt64());
    }

    [Fact]
    public void MultipleReads_AreSequential()
    {
        var reader = Make(0x00, 0x01, 0x00, 0x02);
        Assert.Equal((short)1, reader.ReadInt16());
        Assert.Equal((short)2, reader.ReadInt16());
    }
}
