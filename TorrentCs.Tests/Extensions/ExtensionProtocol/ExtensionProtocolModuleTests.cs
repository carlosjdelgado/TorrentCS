using BencodeNET.Objects;
using BencodeNET.Parsing;
using Microsoft.Extensions.Logging.Abstractions;
using TorrentCs.Application.BitTorrent;
using TorrentCs.Application.BitTorrent.ExtensionModule;
using TorrentCs.Data;
using TorrentCs.Data.Pieces;
using TorrentCs.Extensions.ExtensionProtocol;
using TorrentCs.Modularity;
using TorrentCs.Transport;
using TorrentCs.Transport.Tcp;

namespace TorrentCs.Tests.Extensions.ExtensionProtocol;

public class ExtensionProtocolModuleTests
{
    [Fact]
    public void OnPrepareHandshake_SetsExtensionBit()
    {
        var reserved = new byte[8];
        CreateModule().OnPrepareHandshake(new PrepareHandshakeContext(reserved));
        Assert.Equal(ExtensionBit, reserved[5] & ExtensionBit);
    }

    [Fact]
    public void OnPeerConnected_SendsHandshake_WithMessageTypesClientAndPort()
    {
        var harness = new Harness(extensionCapable: true);
        var module = CreateModule(port: 51413, new SpyHandler());

        module.OnPeerConnected(harness.Context);

        var sent = harness.SentMessages.Single(m => m.Id == ExtensionProtocolModule.ExtendedMessageId);
        Assert.Equal(0, sent.Data[0]); // extended message id 0 = handshake
        var dict = ParseHandshakePayload(sent.Data);
        Assert.Equal(1, ((BNumber)((BDictionary)dict["m"])[SpyMessage.Type]).Value);
        Assert.Equal("TorrentCs", ((BString)dict["v"]).ToString());
        Assert.Equal(51413, ((BNumber)dict["p"]).Value);
    }

    [Fact]
    public void OnPeerConnected_PeerWithoutExtension_SendsNothing()
    {
        var harness = new Harness(extensionCapable: false);

        CreateModule(handlers: new SpyHandler()).OnPeerConnected(harness.Context);

        Assert.DoesNotContain(harness.SentMessages, m => m.Id == ExtensionProtocolModule.ExtendedMessageId);
    }

    [Fact]
    public void OnMessageReceived_Handshake_StoresPeerMessageIdsAndPort_AndNotifiesHandlers()
    {
        var harness = new Harness(extensionCapable: true);
        var spy = new SpyHandler();
        var module = CreateModule(handlers: spy);

        var handshake = new BDictionary
        {
            ["m"] = new BDictionary { [SpyMessage.Type] = new BNumber(7) },
            ["p"] = new BNumber(6889),
        };
        harness.ReceiveExtended(extensionId: 0, Bencode(handshake));

        module.OnMessageReceived(harness.Context);

        var ids = (Dictionary<string, byte>)harness.Peer.Values[ExtensionProtocolModule.PeerMessageIdsKey];
        Assert.Equal((byte)7, ids[SpyMessage.Type]);
        Assert.Equal(6889, (int)harness.Peer.Values[ExtensionProtocolModule.PeerListenPortKey]);
        Assert.True(spy.PeerConnectedCalled);
    }

    [Fact]
    public void OnMessageReceived_ExtensionMessage_DeserializesAndDispatchesTyped()
    {
        var harness = new Harness(extensionCapable: true);
        var spy = new SpyHandler();
        var module = CreateModule(handlers: spy);

        // SpyMessage was registered with local id 1, so the peer addresses it as extension id 1.
        harness.ReceiveExtended(extensionId: 1, [0xAA, 0xBB]);
        module.OnMessageReceived(harness.Context);

        Assert.NotNull(spy.Received);
        Assert.Equal([0xAA, 0xBB], spy.Received!.DeserializedData);
    }

    [Fact]
    public void OnMessageReceived_HandlerReply_IsFramedWithPeersMessageId()
    {
        var harness = new Harness(extensionCapable: true);
        // The peer advertised our message type under id 9.
        harness.Peer.Values[ExtensionProtocolModule.PeerMessageIdsKey] =
            new Dictionary<string, byte> { [SpyMessage.Type] = 9 };
        var module = CreateModule(handlers: new ReplyingHandler([0x01, 0x02]));

        harness.ReceiveExtended(extensionId: 1, [0xAA]);
        module.OnMessageReceived(harness.Context);

        var sent = harness.SentMessages.Single(m => m.Id == ExtensionProtocolModule.ExtendedMessageId);
        Assert.Equal(9, sent.Data[0]); // framed with the peer's id, not our local id
        Assert.Equal([0x01, 0x02], sent.Data[1..]);
    }

    [Fact]
    public void OnMessageReceived_UnknownExtensionId_DoesNotThrow()
    {
        var harness = new Harness(extensionCapable: true);
        var module = CreateModule(handlers: new SpyHandler());

        harness.ReceiveExtended(extensionId: 200, [0x01]);
        module.OnMessageReceived(harness.Context); // must not throw
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private const byte ExtensionBit = 0x10;

    private static ExtensionProtocolModule CreateModule(
        int port = 6881, params IExtensionProtocolMessageHandler[] handlers) =>
        new(NullLogger<ExtensionProtocolModule>.Instance,
            new LocalTcpConnectionOptions { Port = port }, handlers);

    private static byte[] Bencode(BDictionary dict)
    {
        using var ms = new MemoryStream();
        dict.EncodeTo(ms);
        return ms.ToArray();
    }

    private static BDictionary ParseHandshakePayload(byte[] payload) =>
        new BencodeParser().Parse<BDictionary>(new MemoryStream(payload[1..]));

    private sealed class SpyMessage : IExtensionProtocolMessage
    {
        public const string Type = "spy";
        public string MessageType => Type;
        public byte[]? DeserializedData { get; private set; }
        public byte[] Serialize() => [];
        public void Deserialize(byte[] data) => DeserializedData = data;
    }

    private sealed class SpyHandler : IExtensionProtocolMessageHandler
    {
        public bool PeerConnectedCalled { get; private set; }
        public SpyMessage? Received { get; private set; }

        public IReadOnlyDictionary<string, Func<IExtensionProtocolMessage>> SupportedMessageTypes { get; } =
            new Dictionary<string, Func<IExtensionProtocolMessage>> { [SpyMessage.Type] = () => new SpyMessage() };

        public void PeerConnected(IExtensionProtocolPeerContext context) => PeerConnectedCalled = true;
        public void MessageReceived(IExtensionProtocolMessageReceivedContext context) =>
            Received = (SpyMessage)context.Message;
    }

    private sealed class ReplyingHandler(byte[] replyPayload) : IExtensionProtocolMessageHandler
    {
        public IReadOnlyDictionary<string, Func<IExtensionProtocolMessage>> SupportedMessageTypes { get; } =
            new Dictionary<string, Func<IExtensionProtocolMessage>> { [SpyMessage.Type] = () => new SpyMessage() };

        public void MessageReceived(IExtensionProtocolMessageReceivedContext context) =>
            context.SendMessage(new ReplyMessage(replyPayload));

        private sealed class ReplyMessage(byte[] payload) : IExtensionProtocolMessage
        {
            public string MessageType => SpyMessage.Type;
            public byte[] Serialize() => payload;
            public void Deserialize(byte[] data) { }
        }
    }

    private sealed class Harness
    {
        public Harness(bool extensionCapable)
        {
            Metainfo = new MetainfoBuilder("test").AddFile("f.bin", new byte[64]).WithPieceSize(64).Build();
            DataHandler = new PieceCheckerHandler(new BlockDataHandler(new MemoryFileHandler(), Metainfo), Metainfo);
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
        public T GetValue<T>(string key) => h.Peer.Values.TryGetValue(key, out var v) ? (T)v : default!;
        public void SetValue<T>(string key, T value) => h.Peer.Values[key] = value!;
        public void RegisterMessageHandler(byte messageId) { }
        public void PeersAvailable(IEnumerable<ITransportStream> peers) { }
    }
}
