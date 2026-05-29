using BencodeNET.Objects;
using BencodeNET.Parsing;
using Microsoft.Extensions.Logging;
using TorrentCs.Modularity;

namespace TorrentCs.Application.BitTorrent.ExtensionModule;

/// <summary>
/// Implements the BitTorrent extension protocol (BEP 10): advertises support in the handshake's
/// reserved bytes, exchanges the extended handshake, and dispatches extended messages to the
/// registered <see cref="IBitTorrentExtension"/>s.
/// </summary>
public class ExtensionProtocolModule : IModule
{
    /// <summary>BitTorrent message id used for all extended messages.</summary>
    public const byte ExtendedMessageId = 20;

    /// <summary>The peer-state key under which the peer's advertised extensions are stored.</summary>
    public const string PeerExtensionsKey = "bep10.peer_extensions";

    private const byte HandshakeExtensionId = 0;
    private const byte ExtensionBit = 0x10; // reserved[5] & 0x10

    private readonly ILogger<ExtensionProtocolModule> _logger;
    private readonly IReadOnlyList<IBitTorrentExtension> _extensions;
    private readonly Dictionary<byte, IBitTorrentExtension> _byLocalId = [];
    private readonly Dictionary<string, byte> _localIds = [];

    public ExtensionProtocolModule(
        ILogger<ExtensionProtocolModule> logger, IEnumerable<IBitTorrentExtension> extensions)
    {
        _logger = logger;
        _extensions = extensions.ToList();

        byte id = 1;
        foreach (var ext in _extensions)
        {
            _byLocalId[id] = ext;
            _localIds[ext.Name] = id;
            id++;
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
        SendExtendedHandshake(context);
    }

    public void OnMessageReceived(IMessageReceivedContext context)
    {
        if (context.MessageId != ExtendedMessageId) return;

        int payloadLength = context.MessageLength - 1;
        if (payloadLength < 1) return;

        byte extensionId = context.Reader.ReadByte();
        var data = context.Reader.ReadBytes(payloadLength - 1);

        if (extensionId == HandshakeExtensionId)
            HandleExtendedHandshake(context, data);
        else if (_byLocalId.TryGetValue(extensionId, out var extension))
            extension.OnMessageReceived(context, data);
        else
            _logger.LogDebug("Received unknown extended message id {ExtensionId}", extensionId);
    }

    private void SendExtendedHandshake(IPeerContext context)
    {
        var m = new BDictionary();
        foreach (var (name, id) in _localIds)
            m[name] = new BNumber(id);

        var handshake = new BDictionary
        {
            ["m"] = m,
            ["v"] = new BString("TorrentCs"),
        };

        using var ms = new MemoryStream();
        ms.WriteByte(HandshakeExtensionId);
        handshake.EncodeTo(ms);
        context.SendMessage(ExtendedMessageId, ms.ToArray());
    }

    private void HandleExtendedHandshake(IPeerContext context, byte[] data)
    {
        try
        {
            var dict = new BencodeParser().Parse<BDictionary>(new MemoryStream(data));
            var peerExtensions = new Dictionary<string, byte>();

            if (dict.ContainsKey("m") && dict["m"] is BDictionary m)
            {
                foreach (var (key, value) in m)
                {
                    if (value is BNumber number)
                        peerExtensions[key.ToString()] = (byte)number.Value;
                }
            }

            context.SetValue(PeerExtensionsKey, peerExtensions);
            _logger.LogDebug("Peer supports extensions: {Names}",
                peerExtensions.Count > 0 ? string.Join(", ", peerExtensions.Keys) : "(none)");
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse extended handshake");
        }
    }
}
