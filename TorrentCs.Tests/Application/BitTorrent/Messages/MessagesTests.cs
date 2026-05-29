using TorrentCs.Application.BitTorrent;
using TorrentCs.Application.BitTorrent.Messages;
using TorrentCs.Data;
using TorrentCs.Transport;

namespace TorrentCs.Tests.Application.BitTorrent.Messages;

public class MessagesTests
{
    private static (BigEndianBinaryWriter Writer, MemoryStream Stream) MakeWriter()
    {
        var ms = new MemoryStream();
        return (new BigEndianBinaryWriter(ms), ms);
    }

    private static BinaryReader MakeReader(byte[] data) =>
        new(new MemoryStream(data));

    private static BigEndianBinaryReader MakeBeReader(byte[] data) =>
        new(new MemoryStream(data));

    // ─── Choke / Unchoke / Interested / NotInterested ────────────────────────

    [Theory]
    [InlineData(typeof(ChokeMessage), ChokeMessage.MessageID)]
    [InlineData(typeof(UnchokeMessage), UnchokeMessage.MessageID)]
    [InlineData(typeof(InterestedMessage), InterestedMessage.MessageID)]
    [InlineData(typeof(NotInterestedMessage), NotInterestedMessage.MessageID)]
    public void SimpleMessages_HaveCorrectId(Type type, byte expectedId)
    {
        var msg = (CommonPeerMessage)Activator.CreateInstance(type)!;
        Assert.Equal(expectedId, msg.ID);
    }

    // ─── HaveMessage ─────────────────────────────────────────────────────────

    [Fact]
    public void HaveMessage_Send_WritesPieceIndex()
    {
        var (writer, ms) = MakeWriter();
        new HaveMessage(new Piece(42)).Send(new BinaryWriter(ms));
        var bytes = ms.ToArray();
        // First byte = ID (4), then 4 bytes big-endian index = 42
        Assert.Equal(HaveMessage.MessageID, bytes[0]);
        Assert.Equal(42, (bytes[1] << 24) | (bytes[2] << 16) | (bytes[3] << 8) | bytes[4]);
    }

    [Fact]
    public void HaveMessage_Receive_ReadsPieceIndex()
    {
        using var ms = new MemoryStream();
        new BigEndianBinaryWriter(ms).Write(7);
        ms.Seek(0, SeekOrigin.Begin);

        var msg = new HaveMessage();
        msg.Receive(new BinaryReader(ms), 4);
        Assert.Equal(7, msg.Piece!.Index);
    }

    // ─── BitfieldMessage ─────────────────────────────────────────────────────

    [Fact]
    public void BitfieldMessage_Send_WritesIdAndBytes()
    {
        var bf = new Bitfield(8); bf[0] = true;
        using var ms = new MemoryStream();
        new BitfieldMessage(bf).Send(new BinaryWriter(ms));
        var bytes = ms.ToArray();
        Assert.Equal(BitfieldMessage.MessageID, bytes[0]);
        Assert.Equal(0b10000000, bytes[1]);
    }

    [Fact]
    public void BitfieldMessage_Receive_RestoresBits()
    {
        var originalBf = new Bitfield(8); originalBf[3] = true;
        var rawBytes = originalBf.ToBytes();

        var msg = new BitfieldMessage(8);
        msg.Receive(new BinaryReader(new MemoryStream(rawBytes)), rawBytes.Length);

        Assert.True(msg.Bitfield![3]);
        Assert.False(msg.Bitfield[0]);
    }

    // ─── RequestMessage ───────────────────────────────────────────────────────

    [Fact]
    public void RequestMessage_Send_WritesBlockRequest()
    {
        using var ms = new MemoryStream();
        new RequestMessage(new BlockRequest(1, 16384, 16384)).Send(new BinaryWriter(ms));
        var bytes = ms.ToArray();
        Assert.Equal(RequestMessage.MessageID, bytes[0]);
        Assert.Equal(1, MakeBeReader(bytes[1..]).ReadInt32());
    }

    [Fact]
    public void RequestMessage_Receive_ReadsBlockRequest()
    {
        using var ms = new MemoryStream();
        var be = new BigEndianBinaryWriter(ms);
        be.Write(2); // pieceIndex
        be.Write(0); // offset
        be.Write(16384); // length
        ms.Seek(0, SeekOrigin.Begin);

        var msg = new RequestMessage();
        msg.Receive(new BinaryReader(ms), 12);
        Assert.Equal(2, msg.Block!.PieceIndex);
        Assert.Equal(16384, msg.Block.Length);
    }

    // ─── PieceMessage ─────────────────────────────────────────────────────────

    [Fact]
    public void PieceMessage_Receive_ReadsBlockData()
    {
        var data = new byte[] { 1, 2, 3, 4 };
        using var ms = new MemoryStream();
        var be = new BigEndianBinaryWriter(ms);
        be.Write(0); // pieceIndex
        be.Write(0); // offset
        ms.Write(data);
        ms.Seek(0, SeekOrigin.Begin);

        var msg = new PieceMessage();
        msg.Receive(new BinaryReader(ms), 8 + data.Length);

        Assert.Equal(data, msg.Block!.Data);
        Assert.Equal(0, msg.Block.PieceIndex);
    }

    // ─── CancelMessage ────────────────────────────────────────────────────────

    [Fact]
    public void CancelMessage_Id_IsEight()
    {
        Assert.Equal((byte)8, CancelMessage.MessageID);
    }

    [Fact]
    public void CancelMessage_Receive_ReadsBlock()
    {
        using var ms = new MemoryStream();
        var be = new BigEndianBinaryWriter(ms);
        be.Write(3);
        be.Write(512);
        be.Write(16384);
        ms.Seek(0, SeekOrigin.Begin);

        var msg = new CancelMessage();
        msg.Receive(new BinaryReader(ms), 12);
        Assert.Equal(3, msg.Block!.PieceIndex);
        Assert.Equal(512, msg.Block.Offset);
    }

    // ─── KeepAliveMessage ─────────────────────────────────────────────────────

    [Fact]
    public void KeepAliveMessage_Send_WritesZeroLength()
    {
        using var ms = new MemoryStream();
        new KeepAliveMessage().Send(new BinaryWriter(ms));
        var bytes = ms.ToArray();
        Assert.Equal(4, bytes.Length);
        Assert.All(bytes, b => Assert.Equal(0, b));
    }
}
