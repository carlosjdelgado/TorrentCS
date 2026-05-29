using TorrentCs.Application.BitTorrent;
using TorrentCs.Modularity;

namespace TorrentCs.Extensions.ExtensionProtocol;

/// <summary>
/// Context handed to an extension for a specific peer. Adds extension-protocol operations on top of
/// the generic <see cref="IPeerContext"/>, so the extension can send typed messages without knowing
/// the wire format.
/// </summary>
public interface IExtensionProtocolPeerContext : IPeerContext
{
    /// <summary>The message type names this peer advertised support for in its handshake.</summary>
    IReadOnlyCollection<string> PeerSupportedMessageTypes { get; }

    /// <summary>Sends a typed extension message to this peer, framed with the peer's own message id.</summary>
    void SendMessage(IExtensionProtocolMessage message);

    /// <summary>
    /// The TCP listen port a peer advertised via BEP 10 "p", or <c>null</c> if it did not. Lets an
    /// extension report a peer with its reachable port rather than its ephemeral connection port.
    /// </summary>
    int? GetListenPort(IPeer peer);
}
