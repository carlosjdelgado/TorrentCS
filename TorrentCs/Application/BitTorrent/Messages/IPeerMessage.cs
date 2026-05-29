namespace TorrentCs.Application.BitTorrent.Messages;

public interface IPeerMessage
{
    void Send(BinaryWriter writer);
    void Receive(BinaryReader reader, int length);
}
