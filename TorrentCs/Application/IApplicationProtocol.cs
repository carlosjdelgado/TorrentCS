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
    void AcceptConnection(AcceptPeerConnectionEventArgs args);
    void PieceCompleted(Piece piece);
    void PieceCorrupted(Piece piece);
    void PeersAvailable(IEnumerable<ITransportStream> peers);
}
