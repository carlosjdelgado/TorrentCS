using BencodeNET.Objects;
using Microsoft.Extensions.Logging.Abstractions;
using TorrentCs.Application.BitTorrent;
using TorrentCs.Data;
using TorrentCs.Data.Pieces;
using TorrentCs.Extensions.ExtensionProtocol;
using TorrentCs.Extensions.SendMetadata;
using TorrentCs.Modularity;
using TorrentCs.TorrentParsers;
using TorrentCs.Transport;

namespace TorrentCs.Tests.Extensions.SendMetadata;

public class MetadataMessageHandlerTests
{
    [Fact]
    public void PrepareHandshake_WithMetadata_AdvertisesMetadataSize()
    {
        var metainfo = BuildMetainfo(contentPieces: 3);
        var ctx = new FakePrepareContext { Metainfo = metainfo };

        CreateHandler().PrepareExtensionProtocolHandshake(ctx);

        Assert.Equal(metainfo.RawInfoDict.Length, (int)((BNumber)ctx.HandshakeContent["metadata_size"]).Value);
    }

    [Fact]
    public void PrepareHandshake_WithoutMetadata_DoesNotAdvertise()
    {
        // A partial metainfo (e.g. started from an info-hash) has no raw info dictionary yet.
        var partial = new Metainfo("t", new Sha1Hash(new byte[20]), [], [], 16384, [], []);
        var ctx = new FakePrepareContext { Metainfo = partial };

        CreateHandler().PrepareExtensionProtocolHandshake(ctx);

        Assert.False(ctx.HandshakeContent.ContainsKey("metadata_size"));
    }

    [Fact]
    public void MessageReceived_Request_RespondsWithDataPiece()
    {
        var metainfo = BuildMetainfo(contentPieces: 3); // small: metadata fits in one piece
        var ctx = new FakeReceivedContext
        {
            Metainfo = metainfo,
            Message = new MetadataMessage { RequestType = MetadataMessage.MessageType.Request, PieceIndex = 0 },
        };

        CreateHandler().MessageReceived(ctx);

        var reply = (MetadataMessage)ctx.Sent.Single();
        Assert.Equal(MetadataMessage.MessageType.Data, reply.RequestType);
        Assert.Equal(0, reply.PieceIndex);
        Assert.Equal(metainfo.RawInfoDict.Length, reply.TotalSize);
        Assert.Equal(metainfo.RawInfoDict, reply.PieceData);
    }

    [Fact]
    public void MessageReceived_RequestsAcrossMultiplePieces_ReassembleToRawInfoDict()
    {
        var metainfo = BuildMetainfo(contentPieces: 2000); // large: metadata spans several pieces
        Assert.True(metainfo.RawInfoDict.Length > MetadataMessage.PieceSize);
        var handler = CreateHandler();

        var reassembled = new List<byte>();
        int pieceCount = (metainfo.RawInfoDict.Length + MetadataMessage.PieceSize - 1) / MetadataMessage.PieceSize;
        for (int i = 0; i < pieceCount; i++)
        {
            var ctx = new FakeReceivedContext
            {
                Metainfo = metainfo,
                Message = new MetadataMessage { RequestType = MetadataMessage.MessageType.Request, PieceIndex = i },
            };
            handler.MessageReceived(ctx);
            reassembled.AddRange(((MetadataMessage)ctx.Sent.Single()).PieceData);
        }

        Assert.Equal(metainfo.RawInfoDict, reassembled.ToArray());
    }

    [Fact]
    public void MessageReceived_RequestOutOfRange_RespondsReject()
    {
        var ctx = new FakeReceivedContext
        {
            Metainfo = BuildMetainfo(contentPieces: 3),
            Message = new MetadataMessage { RequestType = MetadataMessage.MessageType.Request, PieceIndex = 99 },
        };

        CreateHandler().MessageReceived(ctx);

        Assert.Equal(MetadataMessage.MessageType.Reject, ((MetadataMessage)ctx.Sent.Single()).RequestType);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static MetadataMessageHandler CreateHandler() =>
        new(NullLogger<MetadataMessageHandler>.Instance);

    // Builds a real metainfo via the parser so RawInfoDict is populated. The metadata size is driven
    // by the number of content pieces (the "pieces" string is 20 bytes each).
    private static Metainfo BuildMetainfo(int contentPieces)
    {
        const int pieceLength = 16384;
        var info = new BDictionary
        {
            ["name"] = new BString("f.bin"),
            ["piece length"] = new BNumber(pieceLength),
            ["length"] = new BNumber((long)contentPieces * pieceLength),
            ["pieces"] = new BString(new byte[contentPieces * 20]),
        };
        var torrent = new BDictionary { ["info"] = info, ["announce"] = new BString("http://t/announce") };

        using var ms = new MemoryStream();
        torrent.EncodeTo(ms);
        ms.Position = 0;
        return TorrentParser.ReadFromStream(ms);
    }

    private sealed class AddressPeer(string address) : IPeer
    {
        public PeerId PeerId { get; } = new(new byte[20]);
        public string Address { get; } = address;
        public Bitfield Available { get; } = new(0);
    }

    private sealed class FakePrepareContext : IPrepareExtensionProtocolHandshakeContext
    {
        public BDictionary HandshakeContent { get; } = [];
        public Metainfo Metainfo { get; init; } = null!;
    }

    private sealed class FakeReceivedContext : IExtensionProtocolMessageReceivedContext
    {
        public IExtensionProtocolMessage Message { get; init; } = null!;
        public Metainfo Metainfo { get; init; } = null!;
        public IPeer Peer { get; init; } = new AddressPeer("fake:0");
        public List<IExtensionProtocolMessage> Sent { get; } = [];

        public void SendMessage(IExtensionProtocolMessage message) => Sent.Add(message);

        public IReadOnlyCollection<IPeer> Peers => [Peer];
        public IReadOnlyCollection<string> PeerSupportedMessageTypes => ["ut_metadata"];
        public int? GetListenPort(IPeer peer) => null;
        public T GetValue<T>(string key) => default!;
        public void SetValue<T>(string key, T value) { }
        public void PeersAvailable(IEnumerable<ITransportStream> peers) { }

        public IPieceDataHandler DataHandler => throw new NotSupportedException();
        public IBlockRequests BlockRequests => throw new NotSupportedException();
        public void RegisterMessageHandler(byte messageId) => throw new NotSupportedException();
        public void SendMessage(byte messageId, byte[] data) => throw new NotSupportedException();
    }
}
