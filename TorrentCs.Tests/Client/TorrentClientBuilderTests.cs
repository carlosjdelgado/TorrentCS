using TorrentCs;
using TorrentCs.Application.BitTorrent;

namespace TorrentCs.Tests.Client;

[Collection("TorrentClient")]
public class TorrentClientBuilderTests
{
    [Fact]
    public void CreateDefaultBuilder_BuildsClient()
    {
        using var client = TorrentClientBuilder.CreateDefaultBuilder().Build();
        Assert.NotNull(client);
    }

    [Fact]
    public void UsePeerId_SetsLocalPeerId()
    {
        var peerId = PeerId.CreateNew();
        using var client = TorrentClientBuilder.CreateDefaultBuilder()
            .UsePeerId(peerId)
            .Build();

        Assert.Equal(peerId.Value, client.LocalPeerId.Value);
    }

    [Fact]
    public void UsePort_DoesNotThrow()
    {
        using var client = TorrentClientBuilder.CreateDefaultBuilder()
            .UsePort(0)
            .Build();

        Assert.NotNull(client);
    }

    [Fact]
    public void FluentApi_ChainsMethods()
    {
        var builder = TorrentClientBuilder.CreateDefaultBuilder();
        var result = builder.UsePort(0).UsePeerId(PeerId.CreateNew());
        Assert.Same(builder, result);
    }

    [Fact]
    public void ConfigureServices_IsInvoked()
    {
        bool invoked = false;
        using var client = TorrentClientBuilder.CreateDefaultBuilder()
            .ConfigureServices(_ => invoked = true)
            .Build();

        Assert.True(invoked);
    }
}
