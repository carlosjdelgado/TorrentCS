using TorrentCs.Application.BitTorrent;
using TorrentCs.Application.BitTorrent.Messages;
using TorrentCs.Data;
using TorrentCs.Data.Pieces;
using TorrentCs.Modularity;
using TorrentCs.Transport;

namespace TorrentCs.Tests.Application.BitTorrent;

public class CoreMessagingModuleTests
{
    [Fact]
    public void OnMessageReceived_BitfieldWithNeededPieces_SendsInterested()
    {
        var harness = new Harness(pieceCount: 4);

        // Peer advertises it has piece 0 (which we don't have)
        var peerBitfield = new Bitfield(4);
        peerBitfield.SetPieceAvailable(0, true);

        harness.Receive(BitfieldMessage.MessageID, peerBitfield.ToBytes());

        Assert.Contains(harness.SentMessages, m => m.Id == InterestedMessage.MessageID);
        Assert.True(harness.Peer.IsInterestedInPeer);
    }

    [Fact]
    public void OnMessageReceived_BitfieldWeAlreadyHaveEverything_DoesNotSendInterested()
    {
        var harness = new Harness(pieceCount: 2, completeAllPieces: true);

        var peerBitfield = new Bitfield(2);
        peerBitfield.SetAll(true);

        harness.Receive(BitfieldMessage.MessageID, peerBitfield.ToBytes());

        Assert.DoesNotContain(harness.SentMessages, m => m.Id == InterestedMessage.MessageID);
        Assert.False(harness.Peer.IsInterestedInPeer);
    }

    [Fact]
    public void OnMessageReceived_EmptyBitfield_DoesNotSendInterested()
    {
        var harness = new Harness(pieceCount: 4);

        var peerBitfield = new Bitfield(4); // peer has nothing
        harness.Receive(BitfieldMessage.MessageID, peerBitfield.ToBytes());

        Assert.DoesNotContain(harness.SentMessages, m => m.Id == InterestedMessage.MessageID);
    }

    [Fact]
    public void OnMessageReceived_Interested_ReturnsUnchoke()
    {
        var harness = new Harness(pieceCount: 2);

        harness.Receive(InterestedMessage.MessageID, []);

        Assert.Contains(harness.SentMessages, m => m.Id == UnchokeMessage.MessageID);
        Assert.False(harness.Peer.IsChokingRemotePeer);
        Assert.True(harness.Peer.IsInterestedInRemotePeer);
    }

    [Fact]
    public void OnMessageReceived_InterestedTwice_UnchokesOnce()
    {
        var harness = new Harness(pieceCount: 2);

        harness.Receive(InterestedMessage.MessageID, []);
        harness.Receive(InterestedMessage.MessageID, []);

        Assert.Single(harness.SentMessages, m => m.Id == UnchokeMessage.MessageID);
    }

    [Fact]
    public void OnMessageReceived_HaveForNeededPiece_SendsInterested()
    {
        var harness = new Harness(pieceCount: 4);

        // HaveMessage body = piece index (4 bytes big-endian)
        using var ms = new MemoryStream();
        new BigEndianBinaryWriter(ms).Write(2);
        harness.Receive(HaveMessage.MessageID, ms.ToArray());

        Assert.Contains(harness.SentMessages, m => m.Id == InterestedMessage.MessageID);
    }

    [Fact]
    public void OnMessageReceived_Unchoke_ClearsChokedFlag()
    {
        var harness = new Harness(pieceCount: 2);
        Assert.True(harness.Peer.IsChokedByRemotePeer); // choked by default

        harness.Receive(UnchokeMessage.MessageID, []);

        Assert.False(harness.Peer.IsChokedByRemotePeer);
    }

    // ─── Harness ─────────────────────────────────────────────────────────────

    private sealed class Harness
    {
        private readonly CoreMessagingModule _module = new();

        public Harness(int pieceCount, bool completeAllPieces = false)
        {
            var fileData = new byte[pieceCount * 16];
            Metainfo = new MetainfoBuilder("test")
                .AddFile("f.bin", fileData)
                .WithPieceSize(16)
                .Build();

            var fileHandler = new MemoryFileHandler();
            var dataHandler = new PieceCheckerHandler(
                new BlockDataHandler(fileHandler, Metainfo), Metainfo);

            if (completeAllPieces)
                foreach (var piece in Metainfo.Pieces)
                    dataHandler.MarkPieceAsCompleted(piece);

            DataHandler = dataHandler;
            BlockRequests = new BlockRequestManager();
            Peer = new BitTorrentPeer(new FakeStream(), Metainfo);
            Context = new FakeMessageContext(this);
        }

        public Metainfo Metainfo { get; }
        public IPieceDataHandler DataHandler { get; }
        public IBlockRequests BlockRequests { get; }
        public BitTorrentPeer Peer { get; }
        public FakeMessageContext Context { get; }
        public List<(byte Id, byte[] Data)> SentMessages { get; } = [];

        public void Receive(byte messageId, byte[] body)
        {
            Context.MessageId = messageId;
            Context.MessageLength = 1 + body.Length;
            Context.Reader = new BinaryReader(new MemoryStream(body));
            _module.OnMessageReceived(Context);
        }
    }

    private sealed class FakeStream : ITransportStream
    {
        public Stream Stream { get; } = new MemoryStream();
        public bool IsConnected => true;
        public string DisplayAddress => "fake";
        public object Address => "fake";
        public Task Connect() => Task.CompletedTask;
        public void Disconnect() { }
    }

    private sealed class FakeMessageContext : IMessageReceivedContext
    {
        private readonly Harness _h;

        public FakeMessageContext(Harness h) => _h = h;

        public int MessageId { get; set; }
        public int MessageLength { get; set; }
        public BinaryReader Reader { get; set; } = new(new MemoryStream());

        public IPeer Peer => _h.Peer;
        public Metainfo Metainfo => _h.Metainfo;
        public IPieceDataHandler DataHandler => _h.DataHandler;
        public IBlockRequests BlockRequests => _h.BlockRequests;
        public IReadOnlyCollection<IPeer> Peers => [_h.Peer];

        public void SendMessage(byte messageId, byte[] data) => _h.SentMessages.Add((messageId, data));
        public T GetValue<T>(string key) => (T)_h.Peer.Values[key];
        public void SetValue<T>(string key, T value) => _h.Peer.Values[key] = value!;
        public void RegisterMessageHandler(byte messageId) { }
        public void PeersAvailable(IEnumerable<ITransportStream> peers) { }
    }
}
