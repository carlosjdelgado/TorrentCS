using BencodeNET.Objects;
using BencodeNET.Parsing;
using Microsoft.Extensions.Logging.Abstractions;
using TorrentCs.Application.BitTorrent;
using TorrentCs.Application.BitTorrent.ExtensionModule;
using TorrentCs.Data;
using TorrentCs.Data.Pieces;
using TorrentCs.Modularity;
using TorrentCs.Transport;
using TorrentCs.Transport.Tcp;

namespace TorrentCs.Tests.Application.BitTorrent;

public class ExtensionProtocolModuleTests
{
    [Fact]
    public void OnPrepareHandshake_SetsExtensionBit()
    {
        var module = CreateModule();
        var reserved = new byte[8];

        module.OnPrepareHandshake(new PrepareHandshakeContext(reserved));

        Assert.Equal(ExtensionBit, reserved[5] & ExtensionBit);
    }

    [Fact]
    public void OnPeerConnected_PeerSupportsExtension_SendsExtendedHandshake()
    {
        var harness = new Harness(extensionCapable: true);
        var module = CreateModule();

        module.OnPeerConnected(harness.Context);

        var sent = harness.SentMessages.SingleOrDefault(m => m.Id == ExtensionProtocolModule.ExtendedMessageId);
        Assert.NotNull(sent.Data);
        Assert.Equal(0, sent.Data[0]); // extended message id 0 = handshake
    }

    [Fact]
    public void OnPeerConnected_PeerWithoutExtension_SendsNothing()
    {
        var harness = new Harness(extensionCapable: false);
        var module = CreateModule();

        module.OnPeerConnected(harness.Context);

        Assert.DoesNotContain(harness.SentMessages, m => m.Id == ExtensionProtocolModule.ExtendedMessageId);
    }

    [Fact]
    public void OnPeerConnected_AdvertisesRegisteredExtensions()
    {
        var harness = new Harness(extensionCapable: true);
        var module = CreateModule(new SpyExtension("ut_pex"));

        module.OnPeerConnected(harness.Context);

        var sent = harness.SentMessages.Single(m => m.Id == ExtensionProtocolModule.ExtendedMessageId);
        var dict = ParseHandshakePayload(sent.Data);
        var m = (BDictionary)dict["m"];
        Assert.True(m.ContainsKey("ut_pex"));
        Assert.Equal(1, ((BNumber)m["ut_pex"]).Value); // first registered extension gets id 1
    }

    [Fact]
    public void OnPeerConnected_AdvertisesListenPort()
    {
        var harness = new Harness(extensionCapable: true);
        var module = new ExtensionProtocolModule(
            NullLogger<ExtensionProtocolModule>.Instance, [],
            new LocalTcpConnectionOptions { Port = 51413 });

        module.OnPeerConnected(harness.Context);

        var sent = harness.SentMessages.Single(m => m.Id == ExtensionProtocolModule.ExtendedMessageId);
        var dict = ParseHandshakePayload(sent.Data);
        Assert.Equal(51413, ((BNumber)dict["p"]).Value);
    }

    [Fact]
    public void OnMessageReceived_ExtendedHandshake_StoresPeerListenPort()
    {
        var harness = new Harness(extensionCapable: true);
        var module = CreateModule();

        var peerHandshake = new BDictionary
        {
            ["m"] = new BDictionary { ["ut_pex"] = new BNumber(1) },
            ["p"] = new BNumber(6889),
        };
        harness.ReceiveExtended(extensionId: 0, BencodeBytes(peerHandshake));

        module.OnMessageReceived(harness.Context);

        Assert.Equal(6889, (int)harness.Peer.Values[ExtensionProtocolModule.PeerListenPortKey]);
    }

    [Fact]
    public void OnMessageReceived_ExtendedHandshake_StoresPeerExtensions()
    {
        var harness = new Harness(extensionCapable: true);
        var module = CreateModule();

        var peerHandshake = new BDictionary
        {
            ["m"] = new BDictionary { ["ut_metadata"] = new BNumber(3) },
        };
        harness.ReceiveExtended(extensionId: 0, BencodeBytes(peerHandshake));

        module.OnMessageReceived(harness.Context);

        var stored = (Dictionary<string, byte>)harness.Peer.Values[ExtensionProtocolModule.PeerExtensionsKey];
        Assert.Equal((byte)3, stored["ut_metadata"]);
    }

    [Fact]
    public void OnMessageReceived_ExtensionMessage_DispatchesToExtension()
    {
        var spy = new SpyExtension("ut_pex");
        var module = CreateModule(spy);
        var harness = new Harness(extensionCapable: true);

        // ut_pex was registered with local id 1, so the peer addresses it as extension id 1.
        harness.ReceiveExtended(extensionId: 1, [0xAA, 0xBB]);
        module.OnMessageReceived(harness.Context);

        Assert.Equal([0xAA, 0xBB], spy.LastData);
    }

    [Fact]
    public void OnMessageReceived_NonExtendedMessage_Ignored()
    {
        var module = CreateModule();
        var harness = new Harness(extensionCapable: true);
        harness.Context.MessageId = ChokeMessageId; // a non-extended message
        harness.Context.MessageLength = 1;
        harness.Context.Reader = new BinaryReader(new MemoryStream());

        module.OnMessageReceived(harness.Context); // must not throw or consume
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private const byte ExtensionBit = 0x10;
    private const byte ChokeMessageId = 0;

    private static ExtensionProtocolModule CreateModule(params IBitTorrentExtension[] extensions) =>
        new(NullLogger<ExtensionProtocolModule>.Instance, extensions,
            new LocalTcpConnectionOptions { Port = 6881 });

    private static byte[] BencodeBytes(BDictionary dict)
    {
        using var ms = new MemoryStream();
        dict.EncodeTo(ms);
        return ms.ToArray();
    }

    private static BDictionary ParseHandshakePayload(byte[] payload)
    {
        // payload = [extended id byte] + bencode dict
        return new BencodeParser().Parse<BDictionary>(new MemoryStream(payload[1..]));
    }

    private sealed class SpyExtension(string name) : IBitTorrentExtension
    {
        public string Name { get; } = name;
        public byte[]? LastData { get; private set; }
        public void OnMessageReceived(IPeerContext context, byte[] data) => LastData = data;
    }

    private sealed class Harness
    {
        public Harness(bool extensionCapable)
        {
            Metainfo = new MetainfoBuilder("test").AddFile("f.bin", new byte[64]).WithPieceSize(64).Build();
            var fh = new MemoryFileHandler();
            DataHandler = new PieceCheckerHandler(new BlockDataHandler(fh, Metainfo), Metainfo);
            BlockRequests = new BlockRequestManager();
            Peer = new BitTorrentPeer(new FakeStream(), Metainfo);

            var reserved = new byte[8];
            if (extensionCapable) reserved[5] |= ExtensionBit;
            Peer.ApplyRemoteHandshake(reserved, new byte[20]);

            Context = new FakeMessageContext(this);
        }

        public Metainfo Metainfo { get; }
        public IPieceDataHandler DataHandler { get; }
        public IBlockRequests BlockRequests { get; }
        public BitTorrentPeer Peer { get; }
        public FakeMessageContext Context { get; }
        public List<(byte Id, byte[] Data)> SentMessages { get; } = [];

        public void ReceiveExtended(byte extensionId, byte[] data)
        {
            var payload = new byte[1 + data.Length];
            payload[0] = extensionId;
            data.CopyTo(payload, 1);
            Context.MessageId = ExtensionProtocolModule.ExtendedMessageId;
            Context.MessageLength = 1 + payload.Length;
            Context.Reader = new BinaryReader(new MemoryStream(payload));
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

    private sealed class FakeMessageContext(Harness h) : IMessageReceivedContext
    {
        public int MessageId { get; set; }
        public int MessageLength { get; set; }
        public BinaryReader Reader { get; set; } = new(new MemoryStream());

        public IPeer Peer => h.Peer;
        public Metainfo Metainfo => h.Metainfo;
        public IPieceDataHandler DataHandler => h.DataHandler;
        public IBlockRequests BlockRequests => h.BlockRequests;
        public IReadOnlyCollection<IPeer> Peers => [h.Peer];

        public void SendMessage(byte messageId, byte[] data) => h.SentMessages.Add((messageId, data));
        public T GetValue<T>(string key) => (T)h.Peer.Values[key];
        public void SetValue<T>(string key, T value) => h.Peer.Values[key] = value!;
        public void RegisterMessageHandler(byte messageId) { }
        public void PeersAvailable(IEnumerable<ITransportStream> peers) { }
    }
}
