using TorrentCs.Data;
using TorrentCs.Transport;

namespace TorrentCs.Application.BitTorrent.Messages;

public class HaveMessage : CommonPeerMessage
{
    public const byte MessageID = 4;

    public HaveMessage() { }

    public HaveMessage(Piece piece)
    {
        Piece = piece;
    }

    public override byte ID => MessageID;
    public Piece? Piece { get; private set; }

    public override void Send(BinaryWriter writer)
    {
        var be = new BigEndianBinaryWriter(writer.BaseStream);
        be.Write(ID);
        be.Write(Piece!.Index);
        writer.Flush();
    }

    public override void Receive(BinaryReader reader, int length)
    {
        int index = new BigEndianBinaryReader(reader.BaseStream).ReadInt32();
        Piece = new Piece(index);
    }
}
