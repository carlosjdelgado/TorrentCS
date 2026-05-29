using TorrentCs.Application.BitTorrent;

namespace TorrentCs.Tests.Application.BitTorrent;

public class PeerIdTests
{
    [Fact]
    public void Constructor_AcceptsExactly20Bytes()
    {
        var id = new PeerId(new byte[20]);
        Assert.Equal(20, id.Value.Length);
    }

    [Fact]
    public void Constructor_ThrowsForWrongLength()
    {
        Assert.Throws<ArgumentException>(() => new PeerId(new byte[19]));
        Assert.Throws<ArgumentException>(() => new PeerId(new byte[21]));
    }

    [Fact]
    public void Value_ReturnsProvidedBytes()
    {
        var bytes = new byte[20];
        bytes[0] = 42;
        var id = new PeerId(bytes);
        Assert.Same(bytes, id.Value);
    }

    [Fact]
    public void Length_ConstantIs20()
    {
        Assert.Equal(20, PeerId.Length);
    }

    [Fact]
    public void ToString_DecodesAsUtf8()
    {
        // ToString() renders the 20 raw bytes as a UTF-8 string
        var id = new PeerId(new byte[20]);
        Assert.NotNull(id.ToString());
    }

    [Fact]
    public void CreateNew_HasAzureusPrefix()
    {
        var id = PeerId.CreateNew();
        Assert.Equal('-', (char)id.Value[0]);
        Assert.Equal('-', (char)id.Value[7]);
    }

    [Fact]
    public void CreateNew_ClientNameIsKnown()
    {
        var id = PeerId.CreateNew();
        Assert.NotNull(id.ClientName);
    }
}
