using TorrentCs.Data;
using TorrentCs.Data.Pieces;
using TorrentCs.Modularity;
using TorrentCs.Transport;

namespace TorrentCs.Application.BitTorrent.ExtensionModule;

public class PeerContext : IPeerContext
{
    private readonly Action<byte> _registerHandler;
    private readonly ITorrentContext _torrentContext;

    public PeerContext(
        BitTorrentPeer peer,
        Dictionary<string, object> values,
        ITorrentContext torrentContext,
        Action<byte> registerHandler)
    {
        Peer = peer;
        _torrentContext = torrentContext;
        _registerHandler = registerHandler;
    }

    public IPeer Peer { get; }

    public T GetValue<T>(string key) =>
        ((BitTorrentPeer)Peer).Values.TryGetValue(key, out var value) ? (T)value : default!;
    public void SetValue<T>(string key, T value) => ((BitTorrentPeer)Peer).Values[key] = value!;
    public void RegisterMessageHandler(byte messageId) => _registerHandler(messageId);

    public void SendMessage(byte messageId, byte[] data) =>
        ((BitTorrentPeer)Peer).SendMessage(messageId, data);

    // ITorrentContext delegation
    public Metainfo Metainfo => _torrentContext.Metainfo;
    public IReadOnlyCollection<IPeer> Peers => _torrentContext.Peers;
    public IPieceDataHandler DataHandler => _torrentContext.DataHandler;
    public IBlockRequests BlockRequests => _torrentContext.BlockRequests;
    public void PeersAvailable(IEnumerable<ITransportStream> peers) =>
        _torrentContext.PeersAvailable(peers);
}
