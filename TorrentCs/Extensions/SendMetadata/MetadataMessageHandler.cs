using System.Collections.Concurrent;
using System.Security.Cryptography;
using BencodeNET.Objects;
using Microsoft.Extensions.Logging;
using TorrentCs.Data;
using TorrentCs.Extensions.ExtensionProtocol;
using TorrentCs.Modularity;
using TorrentCs.Modularity.MetainfoProvider;
using TorrentCs.TorrentParsers;

namespace TorrentCs.Extensions.SendMetadata;

/// <summary>
/// Metadata exchange (BEP 9, "ut_metadata"). Two roles:
/// <list type="bullet">
/// <item>Serving: advertises the metadata size and answers <c>request</c> messages with slices of
/// our info dictionary.</item>
/// <item>Downloading: when started from an info-hash (no .torrent), requests the metadata pieces
/// from peers, reassembles and verifies them, and rebuilds the full metainfo. Exposed via
/// <see cref="IMetainfoProvider"/>.</item>
/// </list>
/// </summary>
public class MetadataMessageHandler : IExtensionProtocolMessageHandler, IMetainfoProvider
{
    private const string MetadataSizeKey = "metadata_size";

    private readonly ILogger<MetadataMessageHandler> _logger;
    private readonly ConcurrentDictionary<Sha1Hash, MetadataDownload> _downloads = new();

    public MetadataMessageHandler(ILogger<MetadataMessageHandler> logger)
    {
        _logger = logger;
    }

    public IReadOnlyDictionary<string, Func<IExtensionProtocolMessage>> SupportedMessageTypes { get; } =
        new Dictionary<string, Func<IExtensionProtocolMessage>>
        {
            [MetadataMessage.Type] = () => new MetadataMessage(),
        };

    /// <summary>
    /// Downloads the metadata for the given torrent from its peers and returns the rebuilt metainfo.
    /// The caller is expected to have a running protocol that connects to peers for this info-hash.
    /// </summary>
    public Task<Metainfo> GetMetainfo(ITorrentContext context, CancellationToken ct)
    {
        var download = _downloads.GetOrAdd(context.Metainfo.InfoHash,
            infoHash => new MetadataDownload(infoHash, context.Metainfo.Trackers));
        ct.Register(() => download.Completion.TrySetCanceled(ct));
        return download.Completion.Task;
    }

    public void PrepareExtensionProtocolHandshake(IPrepareExtensionProtocolHandshakeContext context)
    {
        // Only advertise a size if we actually have the metadata to serve.
        var raw = context.Metainfo.RawInfoDict;
        if (raw is { Length: > 0 })
            context.HandshakeContent[MetadataSizeKey] = new BNumber(raw.Length);
    }

    public void PeerConnected(IExtensionProtocolPeerContext context)
    {
        // Only act if we are downloading metadata for this torrent and the peer supports ut_metadata.
        if (!_downloads.TryGetValue(context.Metainfo.InfoHash, out var download)) return;
        if (!context.PeerSupportedMessageTypes.Contains(MetadataMessage.Type)) return;

        RequestMissingPieces(context, download);
    }

    public void MessageReceived(IExtensionProtocolMessageReceivedContext context)
    {
        var message = (MetadataMessage)context.Message;
        switch (message.RequestType)
        {
            case MetadataMessage.MessageType.Request:
                ServeRequest(context, message.PieceIndex);
                break;
            case MetadataMessage.MessageType.Data:
                HandleData(context, message);
                break;
            case MetadataMessage.MessageType.Reject:
                // This peer won't serve the piece; another connected peer may.
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

    private void HandleData(IExtensionProtocolMessageReceivedContext context, MetadataMessage message)
    {
        if (!_downloads.TryGetValue(context.Metainfo.InfoHash, out var download)) return;

        var result = download.AddPiece(message.PieceIndex, message.TotalSize, message.PieceData);

        if (result.Complete)
        {
            if (download.Verify(result.Assembled))
            {
                var metainfo = TorrentParser.BuildFromInfoDictionary(result.Assembled, download.Trackers);
                _downloads.TryRemove(download.InfoHash, out _);
                _logger.LogDebug("ut_metadata: downloaded and verified metadata ({Size} bytes)",
                    result.Assembled.Length);
                download.Completion.TrySetResult(metainfo);
            }
            else
            {
                _logger.LogDebug("ut_metadata: assembled metadata failed hash check, retrying");
                download.Reset();
                RequestMissingPieces(context, download);
            }
        }
        else if (result.LearnedSize)
        {
            // Now we know how many pieces there are; request the rest from this peer.
            RequestMissingPieces(context, download);
        }
    }

    private static void RequestMissingPieces(IExtensionProtocolPeerContext context, MetadataDownload download)
    {
        foreach (int pieceIndex in download.MissingPieceIndices())
            context.SendMessage(new MetadataMessage
            {
                RequestType = MetadataMessage.MessageType.Request,
                PieceIndex = pieceIndex,
            });
    }

    /// <summary>Accumulates the metadata pieces for a single torrent while it is being downloaded.</summary>
    private sealed class MetadataDownload
    {
        private readonly object _lock = new();
        private int? _totalSize;
        private byte[]?[] _pieces = [];

        public MetadataDownload(Sha1Hash infoHash, IEnumerable<string> trackers)
        {
            InfoHash = infoHash;
            Trackers = trackers.ToList();
        }

        public Sha1Hash InfoHash { get; }

        public IReadOnlyList<string> Trackers { get; }

        public TaskCompletionSource<Metainfo> Completion { get; } =
            new(TaskCreationOptions.RunContinuationsAsynchronously);

        public (bool LearnedSize, bool Complete, byte[] Assembled) AddPiece(int index, int totalSize, byte[] data)
        {
            lock (_lock)
            {
                bool learnedSize = false;
                if (_totalSize is null && totalSize > 0)
                {
                    _totalSize = totalSize;
                    int count = (totalSize + MetadataMessage.PieceSize - 1) / MetadataMessage.PieceSize;
                    _pieces = new byte[count][];
                    learnedSize = true;
                }

                if (_totalSize is null) return (false, false, []);

                if (index >= 0 && index < _pieces.Length && _pieces[index] is null && data.Length > 0)
                    _pieces[index] = data;

                if (_pieces.All(p => p is not null))
                    return (learnedSize, true, _pieces.SelectMany(p => p!).ToArray());

                return (learnedSize, false, []);
            }
        }

        public IReadOnlyList<int> MissingPieceIndices()
        {
            lock (_lock)
            {
                if (_totalSize is null) return [0]; // need the first piece to learn the total size

                var missing = new List<int>();
                for (int i = 0; i < _pieces.Length; i++)
                    if (_pieces[i] is null)
                        missing.Add(i);
                return missing;
            }
        }

        public bool Verify(byte[] assembled) => new Sha1Hash(SHA1.HashData(assembled)) == InfoHash;

        public void Reset()
        {
            lock (_lock)
            {
                _totalSize = null;
                _pieces = [];
            }
        }
    }
}
