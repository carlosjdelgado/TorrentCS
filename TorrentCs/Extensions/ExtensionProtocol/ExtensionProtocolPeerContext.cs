using TorrentCs.Application.BitTorrent;
using TorrentCs.Data;
using TorrentCs.Data.Pieces;
using TorrentCs.Modularity;
using TorrentCs.Transport;

namespace TorrentCs.Extensions.ExtensionProtocol;

/// <summary>
/// Wraps the generic <see cref="IPeerContext"/> the module receives from the application protocol and
/// adds the extension-protocol operations, so message handlers never see the raw wire format.
/// </summary>
internal class ExtensionProtocolPeerContext : IExtensionProtocolPeerContext
{
    private readonly IPeerContext _inner;
    private readonly Action<IExtensionProtocolMessage> _sendMessage;

    public ExtensionProtocolPeerContext(IPeerContext inner, Action<IExtensionProtocolMessage> sendMessage)
    {
        _inner = inner;
        _sendMessage = sendMessage;
    }

    public IReadOnlyCollection<string> PeerSupportedMessageTypes
    {
        get
        {
            var ids = ExtensionProtocolModule.PeerMessageIds(Peer);
            return ids is null ? [] : ids.Keys.ToList();
        }
    }

    // IPeerContext
    public IPeer Peer => _inner.Peer;
    public T GetValue<T>(string key) => _inner.GetValue<T>(key);
    public void SetValue<T>(string key, T value) => _inner.SetValue(key, value);
    public void RegisterMessageHandler(byte messageId) => _inner.RegisterMessageHandler(messageId);
    public void SendMessage(byte messageId, byte[] data) => _inner.SendMessage(messageId, data);

    // ITorrentContext
    public Metainfo Metainfo => _inner.Metainfo;
    public IReadOnlyCollection<IPeer> Peers => _inner.Peers;
    public IPieceDataHandler DataHandler => _inner.DataHandler;
    public IBlockRequests BlockRequests => _inner.BlockRequests;
    public void PeersAvailable(IEnumerable<ITransportStream> peers) => _inner.PeersAvailable(peers);

    // Extension-protocol operations
    public void SendMessage(IExtensionProtocolMessage message) => _sendMessage(message);

    public int? GetListenPort(IPeer peer) => ExtensionProtocolModule.PeerListenPort(peer);
}
