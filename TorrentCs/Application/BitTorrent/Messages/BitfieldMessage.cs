using TorrentCs.Transport;

namespace TorrentCs.Application.BitTorrent.Messages;

public class BitfieldMessage : CommonPeerMessage
{
    public const byte MessageID = 5;

    private readonly int _pieceCount;

    public BitfieldMessage(int pieceCount)
    {
        _pieceCount = pieceCount;
    }

    public BitfieldMessage(Bitfield bitfield)
    {
        _pieceCount = bitfield.PieceCount;
        Bitfield = bitfield;
    }

    public override byte ID => MessageID;
    public Bitfield? Bitfield { get; private set; }

    public override void Send(BinaryWriter writer)
    {
        var be = new BigEndianBinaryWriter(writer.BaseStream);
        be.Write(ID);
        writer.Write(Bitfield!.ToBytes());
        writer.Flush();
    }

    public override void Receive(BinaryReader reader, int length)
    {
        var data = reader.ReadBytes(length);
        Bitfield = new Bitfield(_pieceCount, data);
    }
}
