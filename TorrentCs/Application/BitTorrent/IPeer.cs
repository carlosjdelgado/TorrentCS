namespace TorrentCs.Application.BitTorrent;

public interface IPeer
{
    PeerId PeerId { get; }
    string Address { get; }
    Bitfield Available { get; }
}
