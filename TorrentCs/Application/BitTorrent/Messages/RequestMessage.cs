using TorrentCs.Transport;

namespace TorrentCs.Application.BitTorrent.Messages;

public class RequestMessage : CommonPeerMessage
{
    public const byte MessageID = 6;

    public RequestMessage() { }

    public RequestMessage(BlockRequest block)
    {
        Block = block;
    }

    public override byte ID => MessageID;
    public BlockRequest? Block { get; private set; }

    public override void Send(BinaryWriter writer)
    {
        var be = new BigEndianBinaryWriter(writer.BaseStream);
        be.Write(ID);
        be.Write(Block!.PieceIndex);
        be.Write(Block.Offset);
        be.Write(Block.Length);
        writer.Flush();
    }

    public override void Receive(BinaryReader reader, int length)
    {
        var be = new BigEndianBinaryReader(reader.BaseStream);
        Block = new BlockRequest(be.ReadInt32(), be.ReadInt32(), be.ReadInt32());
    }
}
