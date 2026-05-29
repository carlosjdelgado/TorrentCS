using BencodeNET.Objects;
using Microsoft.Extensions.Logging;
using TorrentCs.Extensions.ExtensionProtocol;

namespace TorrentCs.Extensions.SendMetadata;

/// <summary>
/// Metadata exchange (BEP 9, "ut_metadata"). Serves our metadata to peers that don't have the
/// .torrent: it advertises the metadata size in the extended handshake and answers each
/// <c>request</c> with the corresponding 16 KiB slice of the info dictionary.
/// </summary>
public class MetadataMessageHandler : IExtensionProtocolMessageHandler
{
    private const string MetadataSizeKey = "metadata_size";

    private readonly ILogger<MetadataMessageHandler> _logger;

    public MetadataMessageHandler(ILogger<MetadataMessageHandler> logger)
    {
        _logger = logger;
    }

    public IReadOnlyDictionary<string, Func<IExtensionProtocolMessage>> SupportedMessageTypes { get; } =
        new Dictionary<string, Func<IExtensionProtocolMessage>>
        {
            [MetadataMessage.Type] = () => new MetadataMessage(),
        };

    public void PrepareExtensionProtocolHandshake(IPrepareExtensionProtocolHandshakeContext context)
    {
        // Only advertise a size if we actually have the metadata to serve.
        var raw = context.Metainfo.RawInfoDict;
        if (raw is { Length: > 0 })
            context.HandshakeContent[MetadataSizeKey] = new BNumber(raw.Length);
    }

    public void MessageReceived(IExtensionProtocolMessageReceivedContext context)
    {
        var message = (MetadataMessage)context.Message;
        switch (message.RequestType)
        {
            case MetadataMessage.MessageType.Request:
                ServeRequest(context, message.PieceIndex);
                break;
            default:
                // Data/Reject are only meaningful while downloading metadata (handled in 7b).
                break;
        }
    }

    private void ServeRequest(IExtensionProtocolMessageReceivedContext context, int pieceIndex)
    {
        var raw = context.Metainfo.RawInfoDict;
        int offset = pieceIndex * MetadataMessage.PieceSize;

        if (raw is not { Length: > 0 } || offset < 0 || offset >= raw.Length)
        {
            context.SendMessage(new MetadataMessage
            {
                RequestType = MetadataMessage.MessageType.Reject,
                PieceIndex = pieceIndex,
            });
            return;
        }

        int length = Math.Min(MetadataMessage.PieceSize, raw.Length - offset);
        _logger.LogDebug("ut_metadata: serving piece {Piece} ({Length} bytes) to {Address}",
            pieceIndex, length, context.Peer.Address);

        context.SendMessage(new MetadataMessage
        {
            RequestType = MetadataMessage.MessageType.Data,
            PieceIndex = pieceIndex,
            TotalSize = raw.Length,
            PieceData = raw[offset..(offset + length)],
        });
    }
}
