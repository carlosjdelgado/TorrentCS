using TorrentCs.Application.BitTorrent;
using TorrentCs.Data;
using TorrentCs.Data.Pieces;
using TorrentCs.Transport;

namespace TorrentCs.Modularity;

public interface ITorrentContext
{
    Metainfo Metainfo { get; }
    IReadOnlyCollection<IPeer> Peers { get; }
    IPieceDataHandler DataHandler { get; }
    IBlockRequests BlockRequests { get; }

    void PeersAvailable(IEnumerable<ITransportStream> peers);
}
