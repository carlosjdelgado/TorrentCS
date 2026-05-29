using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using TorrentCs.Application.BitTorrent;
using TorrentCs.Data;
using TorrentCs.Data.Pieces;
using TorrentCs.Extensions.ExtensionProtocol;
using TorrentCs.Extensions.PeerExchange;
using TorrentCs.Modularity;
using TorrentCs.Transport;
using TorrentCs.Transport.Tcp;

namespace TorrentCs.Tests.Extensions.PeerExchange;

public class PeerExchangeMessageHandlerTests
{
    [Fact]
    public void SupportedMessageTypes_ContainsUtPex()
    {
        Assert.True(CreateHandler().SupportedMessageTypes.ContainsKey("ut_pex"));
    }

    [Fact]
    public void MessageReceived_WithAddedPeers_FeedsThemBackIntoTheSwarm()
    {
        var ctx = new FakeContext
        {
            Message = new PeerExchangeMessage
            {
                Added =
                [
                    new IPEndPoint(IPAddress.Parse("1.2.3.4"), 6881),
                    new IPEndPoint(IPAddress.Parse("10.0.0.5"), 51413),
                ],
            },
        };

        CreateHandler().MessageReceived(ctx);

        var endpoints = ctx.Available.Cast<TcpTransportStream>().Select(s => s.RemoteEndPoint!).ToList();
        Assert.Contains(endpoints, e => Equals(e.Address, IPAddress.Parse("1.2.3.4")) && e.Port == 6881);
        Assert.Contains(endpoints, e => Equals(e.Address, IPAddress.Parse("10.0.0.5")) && e.Port == 51413);
    }

    [Fact]
    public void MessageReceived_GossipsOtherConnectedPeersAsDelta()
    {
        var recipient = new AddressPeer("100.100.100.100:6881");
        var ctx = new FakeContext
        {
            Message = new PeerExchangeMessage(),
            Peer = recipient,
            ConnectedPeers = { recipient, new AddressPeer("1.2.3.4:6881"), new AddressPeer("5.6.7.8:51413") },
        };

        CreateHandler().MessageReceived(ctx);

        var sent = (PeerExchangeMessage)ctx.Sent.Single();
        Assert.Equal(2, sent.Added.Count); // both others; recipient excluded
        Assert.Contains(sent.Added, e => Equals(e.Address, IPAddress.Parse("1.2.3.4")) && e.Port == 6881);
        Assert.DoesNotContain(sent.Added, e => Equals(e.Address, IPAddress.Parse("100.100.100.100")));
    }

    [Fact]
    public void MessageReceived_PrefersAdvertisedListenPort_WhenGossiping()
    {
        var recipient = new AddressPeer("100.100.100.100:6881");
        var other = new AddressPeer("9.9.9.9:1111"); // ephemeral connection port
        var ctx = new FakeContext
        {
            Message = new PeerExchangeMessage(),
            Peer = recipient,
            ConnectedPeers = { recipient, other },
            ListenPorts = { [other] = 6881 }, // advertised listen port differs
        };

        CreateHandler().MessageReceived(ctx);

        var sent = (PeerExchangeMessage)ctx.Sent.Single();
        Assert.Equal(6881, sent.Added.Single().Port); // advertised 6881, not connection 1111
    }

    [Fact]
    public void MessageReceived_WithinGossipInterval_DoesNotGossipAgain()
    {
        var recipient = new AddressPeer("100.100.100.100:6881");
        var ctx = new FakeContext
        {
            Message = new PeerExchangeMessage(),
            Peer = recipient,
            ConnectedPeers = { recipient, new AddressPeer("1.2.3.4:6881") },
        };
        ctx.SetValue(PeerExchangeMetadata.Key, new PeerExchangeMetadata { LastMessageDate = DateTime.UtcNow });

        CreateHandler().MessageReceived(ctx);

        Assert.Empty(ctx.Sent);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static PeerExchangeMessageHandler CreateHandler() =>
        new(NullLogger<PeerExchangeMessageHandler>.Instance, new FakeTcpTransportProtocol());

    private sealed class AddressPeer(string address) : IPeer
    {
        public PeerId PeerId { get; } = new(new byte[20]);
        public string Address { get; } = address;
        public Bitfield Available { get; } = new(0);
    }

    private sealed class FakeContext : IExtensionProtocolMessageReceivedContext
    {
        private readonly Dictionary<string, object> _values = [];

        public IExtensionProtocolMessage Message { get; init; } = null!;
        public IPeer Peer { get; init; } = new AddressPeer("fake:0");
        public List<IPeer> ConnectedPeers { get; } = [];
        public Dictionary<IPeer, int> ListenPorts { get; } = [];
        public List<ITransportStream> Available { get; } = [];
        public List<IExtensionProtocolMessage> Sent { get; } = [];

        public IReadOnlyCollection<IPeer> Peers => ConnectedPeers;
        public IReadOnlyCollection<string> PeerSupportedMessageTypes => ["ut_pex"];

        public void SendMessage(IExtensionProtocolMessage message) => Sent.Add(message);
        public void PeersAvailable(IEnumerable<ITransportStream> peers) => Available.AddRange(peers);
        public int? GetListenPort(IPeer peer) => ListenPorts.TryGetValue(peer, out var p) ? p : null;

        public T GetValue<T>(string key) => _values.TryGetValue(key, out var v) ? (T)v : default!;
        public void SetValue<T>(string key, T value) => _values[key] = value!;

        public Metainfo Metainfo => throw new NotSupportedException();
        public IPieceDataHandler DataHandler => throw new NotSupportedException();
        public IBlockRequests BlockRequests => throw new NotSupportedException();
        public void RegisterMessageHandler(byte messageId) => throw new NotSupportedException();
        public void SendMessage(byte messageId, byte[] data) => throw new NotSupportedException();
    }

    private sealed class FakeTcpTransportProtocol : ITcpTransportProtocol
    {
        public int Port => 0;
        public IReadOnlyCollection<ITransportStream> Streams => [];
        public event Action<AcceptConnectionEventArgs>? AcceptConnectionHandler { add { } remove { } }

        public TcpTransportStream CreateTransportStream(IPAddress remoteAddress, int port) =>
            new(IPAddress.Any, remoteAddress, port);

        public void Start() { }
        public void Stop() { }
    }
}
