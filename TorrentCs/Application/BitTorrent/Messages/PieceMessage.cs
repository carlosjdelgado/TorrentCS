using TorrentCs.Data;
using TorrentCs.Transport;

namespace TorrentCs.Application.BitTorrent.Messages;

public class PieceMessage : CommonPeerMessage
{
    public const byte MessageID = 7;

    public PieceMessage() { }

    public PieceMessage(Block block)
    {
        Block = block;
    }

    public override byte ID => MessageID;
    public Block? Block { get; private set; }

    public override void Send(BinaryWriter writer)
    {
        var be = new BigEndianBinaryWriter(writer.BaseStream);
        be.Write(ID);
        be.Write(Block!.PieceIndex);
        be.Write(Block.Offset);
        writer.Write(Block.Data);
        writer.Flush();
    }

    public override void Receive(BinaryReader reader, int length)
    {
        var be = new BigEndianBinaryReader(reader.BaseStream);
        int pieceIndex = be.ReadInt32();
        int offset = be.ReadInt32();
        var data = reader.ReadBytes(length - 8);
        Block = new Block(pieceIndex, offset, data);
    }
}
