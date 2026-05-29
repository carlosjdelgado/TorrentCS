using System.Net;
using System.Text;
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

public class PexExtensionTests
{
    [Fact]
    public void Name_IsUtPex()
    {
        Assert.Equal("ut_pex", CreateExtension().Name);
    }

    // ─── Receiving ───────────────────────────────────────────────────────────

    [Fact]
    public void OnMessageReceived_WithAddedPeers_FeedsThemBack()
    {
        var ctx = new FakePeerContext();
        // 127.0.0.1:6881 (0x1AE1) and 10.0.0.5:51413 (0xC8D5)
        var added = new byte[] { 127, 0, 0, 1, 0x1A, 0xE1, 10, 0, 0, 5, 0xC8, 0xD5 };
        var dict = new BDictionary { ["added"] = new BString(added, Encoding.Latin1) };

        CreateExtension().OnMessageReceived(ctx, Bencode(dict));

        Assert.Equal(2, ctx.Available.Count);
        var endpoints = ctx.Available.Cast<TcpTransportStream>().Select(s => s.RemoteEndPoint!).ToList();
        Assert.Contains(endpoints, e => Equals(e.Address, IPAddress.Parse("127.0.0.1")) && e.Port == 6881);
        Assert.Contains(endpoints, e => Equals(e.Address, IPAddress.Parse("10.0.0.5")) && e.Port == 51413);
    }

    [Fact]
    public void OnMessageReceived_NoAddedKey_FeedsNothing()
    {
        var ctx = new FakePeerContext();
        CreateExtension().OnMessageReceived(ctx,
            Bencode(new BDictionary { ["dropped"] = new BString(Array.Empty<byte>(), Encoding.Latin1) }));
        Assert.Empty(ctx.Available);
    }

    [Fact]
    public void OnMessageReceived_CorruptData_DoesNotThrow()
    {
        var ctx = new FakePeerContext();
        CreateExtension().OnMessageReceived(ctx, [0x01, 0x02, 0x03]);
        Assert.Empty(ctx.Available);
    }

    // ─── Sending (gossip) ────────────────────────────────────────────────────

    [Fact]
    public void OnTick_SendsOtherConnectedPeers_UsingPeersOwnExtensionId()
    {
        var meta = new MetainfoBuilder("t").AddFile("f", new byte[16]).WithPieceSize(16).Build();
        var recipient = new BitTorrentPeer(new FakeStream(), meta);
        // The peer advertised ut_pex with id 5 in its extended handshake.
        recipient.Values[ExtensionProtocolModule.PeerExtensionsKey] =
            new Dictionary<string, byte> { ["ut_pex"] = 5 };

        var ctx = new FakePeerContext
        {
            PeerOverride = recipient,
            ConnectedPeers = { recipient, new AddressPeer("1.2.3.4:6881"), new AddressPeer("5.6.7.8:51413") },
        };

        CreateExtension().OnTick(ctx);

        var sent = ctx.Sent.Single();
        Assert.Equal(ExtensionProtocolModule.ExtendedMessageId, sent.Id);
        Assert.Equal(5, sent.Data[0]); // addressed with the peer's own ut_pex id

        var dict = new BencodeParser().Parse<BDictionary>(new MemoryStream(sent.Data[1..]));
        var added = ((BString)dict["added"]).Value.ToArray();
        Assert.Equal(12, added.Length); // two peers × 6 bytes (recipient excluded)
        Assert.Equal(new byte[] { 1, 2, 3, 4, 0x1A, 0xE1 }, added[..6]); // 1.2.3.4:6881
    }

    [Fact]
    public void OnTick_PrefersAdvertisedListenPort_OverConnectionPort()
    {
        var meta = new MetainfoBuilder("t").AddFile("f", new byte[16]).WithPieceSize(16).Build();
        var recipient = new BitTorrentPeer(new FakeStream(), meta);
        recipient.Values[ExtensionProtocolModule.PeerExtensionsKey] =
            new Dictionary<string, byte> { ["ut_pex"] = 5 };

        // Connected via ephemeral port 1111, but it advertised listen port 6881 (0x1AE1).
        var other = new BitTorrentPeer(new FakeStream("9.9.9.9:1111"), meta);
        other.Values[ExtensionProtocolModule.PeerListenPortKey] = 6881;

        var ctx = new FakePeerContext { PeerOverride = recipient, ConnectedPeers = { recipient, other } };

        CreateExtension().OnTick(ctx);

        var dict = new BencodeParser().Parse<BDictionary>(new MemoryStream(ctx.Sent.Single().Data[1..]));
        var added = ((BString)dict["added"]).Value.ToArray();
        Assert.Equal(new byte[] { 9, 9, 9, 9, 0x1A, 0xE1 }, added); // advertised 6881, not connection 1111
    }

    [Fact]
    public void OnTick_PeerWithoutUtPex_SendsNothing()
    {
        var meta = new MetainfoBuilder("t").AddFile("f", new byte[16]).WithPieceSize(16).Build();
        var recipient = new BitTorrentPeer(new FakeStream(), meta); // no peer_extensions set

        var ctx = new FakePeerContext
        {
            PeerOverride = recipient,
            ConnectedPeers = { recipient, new AddressPeer("1.2.3.4:6881") },
        };

        CreateExtension().OnTick(ctx);

        Assert.Empty(ctx.Sent);
    }

    [Fact]
    public void OnTick_NoOtherPeers_SendsNothing()
    {
        var meta = new MetainfoBuilder("t").AddFile("f", new byte[16]).WithPieceSize(16).Build();
        var recipient = new BitTorrentPeer(new FakeStream(), meta);
        recipient.Values[ExtensionProtocolModule.PeerExtensionsKey] =
            new Dictionary<string, byte> { ["ut_pex"] = 5 };

        var ctx = new FakePeerContext { PeerOverride = recipient, ConnectedPeers = { recipient } };

        CreateExtension().OnTick(ctx);

        Assert.Empty(ctx.Sent);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static PexExtension CreateExtension() =>
        new(NullLogger<PexExtension>.Instance, new LocalTcpConnectionOptions
        {
            BindAddress = IPAddress.Any,
            PublicAddress = IPAddress.Loopback,
        });

    private static byte[] Bencode(BDictionary dict)
    {
        using var ms = new MemoryStream();
        dict.EncodeTo(ms);
        return ms.ToArray();
    }

    private sealed class FakePeerContext : IPeerContext
    {
        public List<ITransportStream> Available { get; } = [];
        public List<(byte Id, byte[] Data)> Sent { get; } = [];
        public IPeer? PeerOverride { get; init; }
        public List<IPeer> ConnectedPeers { get; } = [];

        public void PeersAvailable(IEnumerable<ITransportStream> peers) => Available.AddRange(peers);
        public void SendMessage(byte messageId, byte[] data) => Sent.Add((messageId, data));

        public IPeer Peer => PeerOverride ?? new AddressPeer("fake");
        public IReadOnlyCollection<IPeer> Peers => ConnectedPeers;
        public Metainfo Metainfo => throw new NotSupportedException();
        public IPieceDataHandler DataHandler => throw new NotSupportedException();
        public IBlockRequests BlockRequests => throw new NotSupportedException();
        public T GetValue<T>(string key) => throw new NotSupportedException();
        public void SetValue<T>(string key, T value) { }
        public void RegisterMessageHandler(byte messageId) { }
    }

    private sealed class AddressPeer(string address) : IPeer
    {
        public PeerId PeerId { get; } = new(new byte[20]);
        public string Address { get; } = address;
        public Bitfield Available { get; } = new(0);
    }

    private sealed class FakeStream(string address = "fake") : ITransportStream
    {
        public Stream Stream { get; } = new MemoryStream();
        public bool IsConnected => true;
        public string DisplayAddress { get; } = address;
        public object Address => DisplayAddress;
        public Task Connect() => Task.CompletedTask;
        public void Disconnect() { }
    }
}
