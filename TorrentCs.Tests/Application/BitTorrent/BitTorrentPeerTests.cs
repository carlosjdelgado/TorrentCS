using System.Text;
using TorrentCs.Application.BitTorrent;

namespace TorrentCs.Tests.Application.BitTorrent;

public class BitTorrentPeerTests
{
    private static MemoryStream BuildHandshake(string protocol, byte[] infoHash, byte[] peerId)
    {
        var ms = new MemoryStream();
        var pstr = Encoding.ASCII.GetBytes(protocol);
        ms.WriteByte((byte)pstr.Length);
        ms.Write(pstr);
        ms.Write(new byte[8]); // reserved
        ms.Write(infoHash);
        ms.Write(peerId);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    [Fact]
    public async Task ReadIncomingHandshake_ParsesInfoHashAndPeerId()
    {
        var infoHash = new byte[20];
        infoHash[0] = 0xAB;
        infoHash[19] = 0xCD;
        var peerId = Encoding.ASCII.GetBytes("-TC0001-123456789012");

        using var stream = BuildHandshake("BitTorrent protocol", infoHash, peerId);
        var handshake = await BitTorrentPeer.ReadIncomingHandshakeAsync(stream);

        Assert.Equal(infoHash, handshake.InfoHash.Value);
        Assert.Equal(peerId, handshake.PeerId);
    }

    [Fact]
    public async Task ReadIncomingHandshake_RejectsWrongProtocolString()
    {
        using var stream = BuildHandshake("Not BitTorrent!!!!!", new byte[20], new byte[20]);
        await Assert.ThrowsAsync<InvalidDataException>(
            () => BitTorrentPeer.ReadIncomingHandshakeAsync(stream));
    }

    [Fact]
    public async Task ReadIncomingHandshake_EmptyStream_Throws()
    {
        using var stream = new MemoryStream();
        await Assert.ThrowsAsync<EndOfStreamException>(
            () => BitTorrentPeer.ReadIncomingHandshakeAsync(stream));
    }
}
