namespace TorrentCs.Application.BitTorrent;

public class BitTorrentPeerDetails
{
    public BitTorrentPeerDetails(string address, PeerId peerId)
    {
        Address = address;
        PeerId = peerId;
    }

    public string Address { get; }
    public PeerId PeerId { get; }
}
