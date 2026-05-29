using TorrentCs.Transport;

namespace TorrentCs.Application.BitTorrent.Messages;

public abstract class CommonPeerMessage : IPeerMessage
{
    public abstract byte ID { get; }

    public virtual void Send(BinaryWriter writer)
    {
        new BigEndianBinaryWriter(writer.BaseStream).Write(ID);
        writer.Flush();
    }

    public virtual void Receive(BinaryReader reader, int length) { }
}
