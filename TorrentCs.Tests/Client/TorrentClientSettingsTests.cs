using System.Net;
using TorrentCs;

namespace TorrentCs.Tests.Client;

public class TorrentClientSettingsTests
{
    [Fact]
    public void Defaults_ListenPortIs6881()
    {
        Assert.Equal(6881, new TorrentClientSettings().ListenPort);
    }

    [Fact]
    public void Defaults_AdapterAddressIsAny()
    {
        Assert.Equal(IPAddress.Any, new TorrentClientSettings().AdapterAddress);
    }

    [Fact]
    public void Defaults_PeerIdIsGenerated()
    {
        Assert.NotNull(new TorrentClientSettings().PeerId);
    }

    [Fact]
    public void Defaults_FindAvailablePortIsFalse()
    {
        Assert.False(new TorrentClientSettings().FindAvailablePort);
    }

    [Fact]
    public void EachInstance_GeneratesDifferentPeerId()
    {
        var a = new TorrentClientSettings();
        var b = new TorrentClientSettings();
        Assert.NotEqual(a.PeerId.Value, b.PeerId.Value);
    }
}
