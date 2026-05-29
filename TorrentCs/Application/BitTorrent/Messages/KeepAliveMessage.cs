using TorrentCs.Transport;

namespace TorrentCs.Application.BitTorrent.Messages;

public class KeepAliveMessage : IPeerMessage
{
    public void Send(BinaryWriter writer)
    {
        new BigEndianBinaryWriter(writer.BaseStream).Write(0); // length = 0
        writer.Flush();
    }

    public void Receive(BinaryReader reader, int length) { }
}
