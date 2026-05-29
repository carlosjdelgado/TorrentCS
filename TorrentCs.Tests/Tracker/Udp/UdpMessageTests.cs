using System.Net;
using TorrentCs.Data;
using TorrentCs.Tracker.Udp;
using TorrentCs.Transport;

namespace TorrentCs.Tests.Tracker.Udp;

public class UdpMessageTests
{
    // ─── ConnectionRequestMessage ────────────────────────────────────────────

    [Fact]
    public void ConnectionRequest_WritesCorrectBytes()
    {
        var msg = new ConnectionRequestMessage
        {
            ConnectionId = 0x41727101980L,
            TransactionId = 0x12345678,
        };

        var bytes = Serialize(msg);

        // connectionId (8 bytes big-endian)
        Assert.Equal(new byte[] { 0x00, 0x00, 0x04, 0x17, 0x27, 0x10, 0x19, 0x80 }, bytes[..8]);
        // action=0 (4 bytes big-endian)
        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x00 }, bytes[8..12]);
        // transactionId (4 bytes big-endian)
        Assert.Equal(new byte[] { 0x12, 0x34, 0x56, 0x78 }, bytes[12..16]);
    }

    // ─── ConnectionResponseMessage ───────────────────────────────────────────

    [Fact]
    public void ConnectionResponse_ReadsCorrectFields()
    {
        // action=0, txId=0x12345678, connectionId=0x41727101980
        var bytes = BuildResponseBytes(
            action: 0,
            transactionId: 0x12345678,
            extra: [0x00, 0x00, 0x04, 0x17, 0x27, 0x10, 0x19, 0x80]);

        var msg = new ConnectionResponseMessage();
        msg.ReadFrom(new BinaryReader(new MemoryStream(bytes)));

        Assert.Equal(0x12345678, msg.TransactionId);
        Assert.Equal(0x41727101980L, msg.ConnectionId);
    }

    [Fact]
    public void ConnectionResponse_WrongAction_Throws()
    {
        var bytes = BuildResponseBytes(action: 1, transactionId: 1, extra: new byte[8]);
        var msg = new ConnectionResponseMessage();
        Assert.Throws<InvalidDataException>(() =>
            msg.ReadFrom(new BinaryReader(new MemoryStream(bytes))));
    }

    // ─── AnnounceRequestMessage ──────────────────────────────────────────────

    [Fact]
    public void AnnounceRequest_WritesCorrectSize()
    {
        var msg = new AnnounceRequestMessage
        {
            ConnectionId = 0x41727101980L,
            TransactionId = 1,
            InfoHash = Sha1Hash.Empty,
            PeerId = new byte[20],
            Port = 6881,
        };

        var bytes = Serialize(msg);
        // 8 + 4 + 4 + 20 + 20 + 8 + 8 + 8 + 4 + 4 + 4 + 4 + 2 = 98
        Assert.Equal(98, bytes.Length);
    }

    [Fact]
    public void AnnounceRequest_Action_IsOne()
    {
        var msg = new AnnounceRequestMessage
        {
            ConnectionId = 0,
            TransactionId = 0,
            InfoHash = Sha1Hash.Empty,
            PeerId = new byte[20],
        };

        var bytes = Serialize(msg);
        // action at bytes 8..12, big-endian = 1
        Assert.Equal(new byte[] { 0x00, 0x00, 0x00, 0x01 }, bytes[8..12]);
    }

    // ─── AnnounceResponseMessage ─────────────────────────────────────────────

    [Fact]
    public void AnnounceResponse_ReadsIntervalAndPeers()
    {
        var peerIp = new byte[] { 127, 0, 0, 1 };
        var peerPort = new byte[] { 0x1B, 0x39 }; // 6969

        var bytes = BuildAnnounceResponseBytes(
            transactionId: 42,
            interval: 1800,
            leechers: 5,
            seeders: 10,
            peers: [.. peerIp, .. peerPort]);

        var msg = new AnnounceResponseMessage();
        msg.ReadFrom(new BinaryReader(new MemoryStream(bytes)));

        Assert.Equal(42, msg.TransactionId);
        Assert.Equal(1800, msg.Interval);
        Assert.Equal(5, msg.Leechers);
        Assert.Equal(10, msg.Seeders);
        Assert.Single(msg.Peers);
        Assert.Equal(IPAddress.Loopback, msg.Peers[0].IPAddress);
        Assert.Equal(6969, msg.Peers[0].Port);
    }

    [Fact]
    public void AnnounceResponse_WrongAction_Throws()
    {
        var bytes = BuildResponseBytes(action: 0, transactionId: 1,
            extra: new byte[12]); // missing interval/leechers/seeders but wrong action
        var msg = new AnnounceResponseMessage();
        Assert.Throws<InvalidDataException>(() =>
            msg.ReadFrom(new BinaryReader(new MemoryStream(bytes))));
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private static byte[] Serialize(UdpTrackerRequestMessage msg)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        msg.WriteTo(writer);
        writer.Flush();
        return ms.ToArray();
    }

    private static byte[] BuildResponseBytes(int action, int transactionId, byte[] extra)
    {
        using var ms = new MemoryStream();
        var be = new BigEndianBinaryWriter(ms);
        be.Write(action);
        be.Write(transactionId);
        ms.Write(extra);
        return ms.ToArray();
    }

    private static byte[] BuildAnnounceResponseBytes(
        int transactionId, int interval, int leechers, int seeders, byte[] peers)
    {
        using var ms = new MemoryStream();
        var be = new BigEndianBinaryWriter(ms);
        be.Write(1); // action = Announce
        be.Write(transactionId);
        be.Write(interval);
        be.Write(leechers);
        be.Write(seeders);
        ms.Write(peers);
        return ms.ToArray();
    }
}
