using Microsoft.Extensions.DependencyInjection;
using TorrentCs.Transport;

namespace TorrentCs.Tests.Client;

/// <summary>
/// Verifies the client wires up port forwarding: it requests a mapping for the real listen port on
/// start and removes it on dispose. Uses a spy so no real router is touched.
/// </summary>
[Collection("TorrentClient")]
public class PortForwardingIntegrationTests
{
    [Fact]
    public void Client_MapsRealListenPort_OnStart()
    {
        var spy = new SpyPortForwarding();

        using var client = TorrentClientBuilder.CreateDefaultBuilder()
            .UsePort(0) // OS-assigned port
            .ConfigureServices(s => s.AddSingleton<IPortForwarding>(spy))
            .Build();

        Assert.NotNull(spy.MappedPort);
        Assert.True(spy.MappedPort > 0, "should map the actual assigned listen port");
    }

    [Fact]
    public void Client_Dispose_RemovesMapping()
    {
        var spy = new SpyPortForwarding();

        var client = TorrentClientBuilder.CreateDefaultBuilder()
            .UsePort(0)
            .ConfigureServices(s => s.AddSingleton<IPortForwarding>(spy))
            .Build();

        client.Dispose();

        Assert.True(spy.Disposed);
    }

    [Fact]
    public void Client_WithoutUsePortForwarding_UsesNoOpByDefault()
    {
        // No spy registered and UsePortForwarding() not called → the default NullPortForwarding is
        // used, so no router is touched. The client should build and dispose cleanly.
        using var client = TorrentClientBuilder.CreateDefaultBuilder()
            .UsePort(0)
            .Build();

        Assert.NotNull(client);
    }

    private sealed class SpyPortForwarding : IPortForwarding
    {
        public int? MappedPort { get; private set; }
        public bool Disposed { get; private set; }

        public void MapPort(int port) => MappedPort = port;

        public void Dispose() => Disposed = true;
    }
}
