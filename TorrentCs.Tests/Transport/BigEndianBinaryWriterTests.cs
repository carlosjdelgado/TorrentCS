using TorrentCs.Transport;

namespace TorrentCs.Tests.Transport;

public class BigEndianBinaryWriterTests
{
    private static (BigEndianBinaryWriter Writer, MemoryStream Stream) Make()
    {
        var ms = new MemoryStream();
        return (new BigEndianBinaryWriter(ms), ms);
    }

    [Fact]
    public void WriteShort_ProducesBigEndianBytes()
    {
        var (writer, ms) = Make();
        writer.Write((short)0x1234);
        Assert.Equal(new byte[] { 0x12, 0x34 }, ms.ToArray());
    }

    [Fact]
    public void WriteShort_NegativeOne_AllOnes()
    {
        var (writer, ms) = Make();
        writer.Write((short)-1);
        Assert.Equal(new byte[] { 0xFF, 0xFF }, ms.ToArray());
    }

    [Fact]
    public void WriteUShort_ProducesBigEndianBytes()
    {
        var (writer, ms) = Make();
        writer.Write((ushort)6969);
        Assert.Equal(new byte[] { 0x1B, 0x39 }, ms.ToArray());
    }

    [Fact]
    public void WriteUShort_MaxValue()
    {
        var (writer, ms) = Make();
        writer.Write(ushort.MaxValue);
        Assert.Equal(new byte[] { 0xFF, 0xFF }, ms.ToArray());
    }

    [Fact]
    public void WriteInt_ProducesBigEndianBytes()
    {
        var (writer, ms) = Make();
        writer.Write(0x01020304);
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04 }, ms.ToArray());
    }

    [Fact]
    public void WriteInt_NegativeOne()
    {
        var (writer, ms) = Make();
        writer.Write(-1);
        Assert.Equal(new byte[] { 0xFF, 0xFF, 0xFF, 0xFF }, ms.ToArray());
    }

    [Fact]
    public void WriteLong_ProducesBigEndianBytes()
    {
        var (writer, ms) = Make();
        writer.Write(0x0102030405060708L);
        Assert.Equal(new byte[] { 0x01, 0x02, 0x03, 0x04, 0x05, 0x06, 0x07, 0x08 }, ms.ToArray());
    }

    [Fact]
    public void WriteLong_NegativeOne()
    {
        var (writer, ms) = Make();
        writer.Write(-1L);
        Assert.Equal(Enumerable.Repeat((byte)0xFF, 8).ToArray(), ms.ToArray());
    }

    [Fact]
    public void WriteAndRead_Roundtrip_Int32()
    {
        var ms = new MemoryStream();
        var writer = new BigEndianBinaryWriter(ms);
        writer.Write(unchecked((int)0xDEADBEEF)); // explicit int cast — hex literal exceeds int.MaxValue

        ms.Seek(0, SeekOrigin.Begin);
        var reader = new BigEndianBinaryReader(ms);
        Assert.Equal(unchecked((int)0xDEADBEEF), reader.ReadInt32());
    }

    [Fact]
    public void WriteAndRead_Roundtrip_UInt16()
    {
        var ms = new MemoryStream();
        var writer = new BigEndianBinaryWriter(ms);
        writer.Write((ushort)1234);

        ms.Seek(0, SeekOrigin.Begin);
        var reader = new BigEndianBinaryReader(ms);
        Assert.Equal((ushort)1234, reader.ReadUInt16());
    }
}
