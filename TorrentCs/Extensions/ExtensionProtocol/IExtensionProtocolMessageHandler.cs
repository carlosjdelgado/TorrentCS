namespace TorrentCs.Extensions.ExtensionProtocol;

/// <summary>
/// Implemented by each BEP 10 extension. The handler declares which message types it supports and
/// reacts to typed messages; it never touches the extension protocol's wire format (message id 20,
/// extended ids, the handshake dictionary), which the <see cref="ExtensionProtocolModule"/> owns.
/// </summary>
public interface IExtensionProtocolMessageHandler
{
    /// <summary>
    /// The message types this extension handles, keyed by the name advertised in the handshake's
    /// <c>m</c> dictionary, each mapped to a factory that creates an empty message to deserialize into.
    /// </summary>
    IReadOnlyDictionary<string, Func<IExtensionProtocolMessage>> SupportedMessageTypes { get; }

    /// <summary>Invoked while the local extended handshake is being built, to add custom fields.</summary>
    void PrepareExtensionProtocolHandshake(IPrepareExtensionProtocolHandshakeContext context) { }

    /// <summary>Invoked once a peer supporting the extension protocol has exchanged its handshake.</summary>
    void PeerConnected(IExtensionProtocolPeerContext context) { }

    /// <summary>Invoked when this extension receives a message from a peer.</summary>
    void MessageReceived(IExtensionProtocolMessageReceivedContext context);
}
