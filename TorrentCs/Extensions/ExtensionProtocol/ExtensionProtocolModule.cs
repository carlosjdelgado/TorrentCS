using Microsoft.Extensions.Logging;
using TorrentCs.Application.BitTorrent;
using TorrentCs.Modularity;
using TorrentCs.Transport.Tcp;

namespace TorrentCs.Extensions.ExtensionProtocol;

/// <summary>
/// Implements the BitTorrent extension protocol (BEP 10). It advertises support in the handshake's
/// reserved bytes, exchanges the extended handshake, and routes extended messages to the registered
/// <see cref="IExtensionProtocolMessageHandler"/>s. It is the only type that touches the extension
/// protocol's wire format; handlers work purely with typed messages.
/// </summary>
public class ExtensionProtocolModule : IModule
{
    /// <summary>BitTorrent message id used for all extended messages.</summary>
    public const byte ExtendedMessageId = 20;

    /// <summary>Peer-state key under which the peer's advertised message-type ids are stored.</summary>
    public const string PeerMessageIdsKey = "bep10.peer_message_ids";

    /// <summary>Peer-state key under which the peer's advertised TCP listen port ("p") is stored.</summary>
    public const string PeerListenPortKey = "bep10.peer_port";

    private const byte HandshakeMessageId = 0;
    private const byte ExtensionBit = 0x10; // reserved[5] & 0x10

    private readonly ILogger<ExtensionProtocolModule> _logger;
    private readonly LocalTcpConnectionOptions _connectionOptions;
    private readonly IReadOnlyList<IExtensionProtocolMessageHandler> _handlers;
    private readonly Dictionary<string, byte> _localMessageIds = [];
    private readonly Dictionary<byte, IExtensionProtocolMessageHandler> _handlersById = [];
    private readonly Dictionary<byte, Func<IExtensionProtocolMessage>> _factoriesById = [];

    public ExtensionProtocolModule(
        ILogger<ExtensionProtocolModule> logger,
        LocalTcpConnectionOptions connectionOptions,
        IEnumerable<IExtensionProtocolMessageHandler> handlers)
    {
        _logger = logger;
        _connectionOptions = connectionOptions;
        _handlers = handlers.ToList();

        byte nextId = 1;
        foreach (var handler in _handlers)
        {
            foreach (var (name, factory) in handler.SupportedMessageTypes)
            {
                _localMessageIds[name] = nextId;
                _handlersById[nextId] = handler;
                _factoriesById[nextId] = factory;
                nextId++;
            }
        }
    }

    public void OnPrepareHandshake(IPrepareHandshakeContext context)
        => context.ReservedBytes[5] |= ExtensionBit;

    public void OnPeerConnected(IPeerContext context)
    {
        var peer = (BitTorrentPeer)context.Peer;
        if (!peer.SupportedExtensions.HasFlag(ProtocolExtension.ExtensionProtocol))
            return;

        context.RegisterMessageHandler(ExtendedMessageId);

        var handshake = new ExtensionProtocolHandshake
        {
            MessageIds = _localMessageIds,
            Client = "TorrentCs",
            // BEP 10 "p": our listen port, so peers can reach us and report us accurately via PEX.
            ListenPort = _connectionOptions.Port,
        };
        var content = handshake.Serialize();

        var prepareContext = new PrepareExtensionProtocolHandshakeContext(content);
        foreach (var handler in _handlers)
            handler.PrepareExtensionProtocolHandshake(prepareContext);

        SendHandshake(context, content);
    }

    public void OnMessageReceived(IMessageReceivedContext context)
    {
        if (context.MessageId != ExtendedMessageId) return;

        int payloadLength = context.MessageLength - 1;
        if (payloadLength < 1) return;

        byte messageId = context.Reader.ReadByte();
        var data = context.Reader.ReadBytes(payloadLength - 1);

        if (messageId == HandshakeMessageId)
        {
            HandleHandshake(context, data);
            var peerContext = new ExtensionProtocolPeerContext(context, msg => SendExtensionMessage(context, msg));
            foreach (var handler in _handlers)
                handler.PeerConnected(peerContext);
            return;
        }

        if (!_handlersById.TryGetValue(messageId, out var messageHandler))
        {
            _logger.LogDebug("Received unknown extended message id {MessageId}", messageId);
            return;
        }

        var message = _factoriesById[messageId]();
        message.Deserialize(data);

        var receivedContext = new ExtensionProtocolMessageReceivedContext(
            context, message, msg => SendExtensionMessage(context, msg));
        messageHandler.MessageReceived(receivedContext);
    }

    /// <summary>The message-type ids a peer advertised, or <c>null</c> if it sent no handshake.</summary>
    internal static Dictionary<string, byte>? PeerMessageIds(IPeer peer) =>
        ((BitTorrentPeer)peer).Values.TryGetValue(PeerMessageIdsKey, out var value)
            ? value as Dictionary<string, byte>
            : null;

    /// <summary>The TCP listen port a peer advertised via "p", or <c>null</c>.</summary>
    internal static int? PeerListenPort(IPeer peer) =>
        ((BitTorrentPeer)peer).Values.TryGetValue(PeerListenPortKey, out var value) && value is int port
            ? port
            : null;

    private void HandleHandshake(IMessageReceivedContext context, byte[] data)
    {
        try
        {
            var handshake = new ExtensionProtocolHandshake();
            handshake.Deserialize(data);

            context.SetValue(PeerMessageIdsKey, handshake.MessageIds);
            if (handshake.ListenPort is int port)
                context.SetValue(PeerListenPortKey, port);

            _logger.LogDebug("Peer supports extensions: {Names}",
                handshake.MessageIds.Count > 0 ? string.Join(", ", handshake.MessageIds.Keys) : "(none)");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse extended handshake");
        }
    }

    private void SendExtensionMessage(IPeerContext context, IExtensionProtocolMessage message)
    {
        var peerMessageIds = PeerMessageIds(context.Peer);
        if (peerMessageIds is null || !peerMessageIds.TryGetValue(message.MessageType, out var peerMessageId))
        {
            _logger.LogDebug("Peer does not support message type {MessageType}", message.MessageType);
            return;
        }

        using var ms = new MemoryStream();
        ms.WriteByte(peerMessageId);
        var payload = message.Serialize();
        ms.Write(payload, 0, payload.Length);
        context.SendMessage(ExtendedMessageId, ms.ToArray());
    }

    private static void SendHandshake(IPeerContext context, BencodeNET.Objects.BDictionary content)
    {
        using var ms = new MemoryStream();
        ms.WriteByte(HandshakeMessageId);
        content.EncodeTo(ms);
        context.SendMessage(ExtendedMessageId, ms.ToArray());
    }
}
