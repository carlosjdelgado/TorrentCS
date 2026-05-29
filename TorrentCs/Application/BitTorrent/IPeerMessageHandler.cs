namespace TorrentCs.Application.BitTorrent;

public interface IPeerMessageHandler
{
    void MessageReceived(byte messageId, int length, BinaryReader reader, BitTorrentPeer peer);
    void PeerDisconnected(BitTorrentPeer peer);
}
