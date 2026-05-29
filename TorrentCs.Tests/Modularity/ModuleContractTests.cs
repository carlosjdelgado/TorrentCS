using TorrentCs.Application.BitTorrent;
using TorrentCs.Data;
using TorrentCs.Data.Pieces;
using TorrentCs.Modularity;
using TorrentCs.Transport;

namespace TorrentCs.Tests.Modularity;

/// <summary>
/// Verifica los contratos de las interfaces de Modularity usando implementaciones fake.
/// Las implementaciones concretas se probarán en el paso 7 (Application).
/// </summary>
public class ModuleContractTests
{
    [Fact]
    public void IModule_CanBeImplemented()
    {
        IModule module = new NoopModule();
        var ctx = new FakePeerContext();

        module.OnPrepareHandshake(new FakePrepareHandshakeContext());
        module.OnPeerConnected(ctx);
        module.OnMessageReceived(new FakeMessageReceivedContext());
    }

    [Fact]
    public void IPeerContext_InheritsFromITorrentContext()
    {
        IPeerContext ctx = new FakePeerContext();
        ITorrentContext torrentCtx = ctx;
        Assert.NotNull(torrentCtx);
    }

    [Fact]
    public void IMessageReceivedContext_InheritsFromIPeerContext()
    {
        IMessageReceivedContext ctx = new FakeMessageReceivedContext();
        IPeerContext peerCtx = ctx;
        ITorrentContext torrentCtx = ctx;
        Assert.NotNull(peerCtx);
        Assert.NotNull(torrentCtx);
    }

    [Fact]
    public void IModule_OnPrepareHandshake_ReceivesReservedBytes()
    {
        var spy = new SpyModule();
        spy.OnPrepareHandshake(new FakePrepareHandshakeContext { ReservedBytes = new byte[8] });
        Assert.NotNull(spy.LastReservedBytes);
        Assert.Equal(8, spy.LastReservedBytes.Length);
    }

    [Fact]
    public void IModule_OnMessageReceived_ReceivesMessageId()
    {
        var spy = new SpyModule();
        spy.OnMessageReceived(new FakeMessageReceivedContext { MessageId = 42 });
        Assert.Equal(42, spy.LastMessageId);
    }

    // ─── Fakes ────────────────────────────────────────────────────────────────

    private class NoopModule : IModule
    {
        public void OnPrepareHandshake(IPrepareHandshakeContext context) { }
        public void OnPeerConnected(IPeerContext context) { }
        public void OnMessageReceived(IMessageReceivedContext context) { }
    }

    private class SpyModule : IModule
    {
        public byte[]? LastReservedBytes { get; private set; }
        public int LastMessageId { get; private set; }

        public void OnPrepareHandshake(IPrepareHandshakeContext context)
            => LastReservedBytes = context.ReservedBytes;

        public void OnPeerConnected(IPeerContext context) { }

        public void OnMessageReceived(IMessageReceivedContext context)
            => LastMessageId = context.MessageId;
    }

    private class FakePrepareHandshakeContext : IPrepareHandshakeContext
    {
        public byte[] ReservedBytes { get; init; } = new byte[8];
    }

    private class FakeTorrentContext : ITorrentContext
    {
        public Metainfo Metainfo { get; } =
            new MetainfoBuilder("test").AddFile("f.bin", new byte[10]).Build();
        public IReadOnlyCollection<IPeer> Peers => [];
        public IPieceDataHandler DataHandler { get; } = null!;
        public IBlockRequests BlockRequests { get; } = null!;
        public void PeersAvailable(IEnumerable<ITransportStream> peers) { }
    }

    private class FakePeerContext : FakeTorrentContext, IPeerContext
    {
        private readonly Dictionary<string, object> _store = [];

        public IPeer Peer { get; } = new FakePeer();

        public T GetValue<T>(string key) => (T)_store[key];
        public void SetValue<T>(string key, T value) => _store[key] = value!;
        public void RegisterMessageHandler(byte messageId) { }
        public void SendMessage(byte messageId, byte[] data) { }
    }

    private class FakeMessageReceivedContext : FakePeerContext, IMessageReceivedContext
    {
        public int MessageId { get; init; }
        public int MessageLength { get; init; }
        public BinaryReader Reader { get; } = new(Stream.Null);
    }

    private class FakePeer : IPeer
    {
        public PeerId PeerId { get; } = new(new byte[20]);
        public string Address => "127.0.0.1:6881";
        public Bitfield Available { get; } = new(0);
    }
}
