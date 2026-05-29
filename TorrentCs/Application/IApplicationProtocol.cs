using TorrentCs.Data;
using TorrentCs.Data.Pieces;
using TorrentCs.Transport;

namespace TorrentCs.Application;

public interface IApplicationProtocol
{
    event Action? DownloadCompleted;

    Metainfo Metainfo { get; }
    IPieceDataHandler DataHandler { get; }
    IBlockRequests BlockRequests { get; }
    IReadOnlyCollection<object> Peers { get; }
    IReadOnlyCollection<ITransportStream> AvailablePeers { get; }
    IReadOnlyCollection<ITransportStream> ConnectingPeers { get; }
    long Uploaded { get; }

    void ConnectToPeer(ITransportStream stream);

    /// <summary>
    /// Accepts an incoming connection that has already been routed to this torrent (its handshake
    /// was read and the info-hash matched). The protocol replies with its own handshake.
    /// </summary>
    void AcceptConnection(ITransportStream stream);

    void PieceCompleted(Piece piece);
    void PieceCorrupted(Piece piece);
    void PeersAvailable(IEnumerable<ITransportStream> peers);

    /// <summary>Recalculates which peers are choked/unchoked. Called periodically by the engine.</summary>
    void UpdateChoking();
}
