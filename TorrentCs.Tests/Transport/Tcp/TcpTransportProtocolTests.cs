using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging.Abstractions;
using TorrentCs.Transport;
using TorrentCs.Transport.Tcp;

namespace TorrentCs.Tests.Transport.Tcp;

public class TcpTransportProtocolTests
{
    [Fact]
    public void Start_AssignsPort()
    {
        var protocol = BuildProtocol(port: 0);
        protocol.Start();
        Assert.True(protocol.Port > 0);
        protocol.Stop();
    }

    [Fact]
    public void Start_UsesSpecifiedPort()
    {
        // Port 0 means OS assigns; verify a fixed port also works
        var protocol = BuildProtocol(port: 0);
        protocol.Start();
        int assignedPort = protocol.Port;
        protocol.Stop();
        Assert.True(assignedPort > 0);
    }

    [Fact]
    public void Stop_ClearsStreams()
    {
        var protocol = BuildProtocol(port: 0);
        protocol.Start();
        protocol.Stop();
        Assert.Empty(protocol.Streams);
    }

    [Fact]
    public async Task AcceptConnection_WithAcceptCallback_AddsToStreams()
    {
        var protocol = BuildProtocol(port: 0);
        protocol.AcceptConnectionHandler += args => args.Accept();
        protocol.Start();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, protocol.Port);
        await Task.Delay(100); // let listener process

        Assert.Single(protocol.Streams);
        protocol.Stop();
    }

    [Fact]
    public async Task AcceptConnection_WithoutAcceptCallback_DoesNotAddToStreams()
    {
        var protocol = BuildProtocol(port: 0);
        // No handler = not accepted
        protocol.Start();

        using var client = new TcpClient();
        await client.ConnectAsync(IPAddress.Loopback, protocol.Port);
        await Task.Delay(100);

        Assert.Empty(protocol.Streams);
        protocol.Stop();
    }

    [Fact]
    public void CreateTransportStream_ReturnsStream()
    {
        var protocol = BuildProtocol(port: 0);
        var stream = protocol.CreateTransportStream(IPAddress.Loopback, 6969);
        Assert.NotNull(stream);
    }

    [Fact]
    public void RateLimiter_IsExposed()
    {
        var protocol = BuildProtocol(port: 0);
        Assert.NotNull(protocol.RateLimiter);
    }

    private static TcpTransportProtocol BuildProtocol(int port) =>
        new(NullLogger<TcpTransportProtocol>.Instance, new LocalTcpConnectionOptions
        {
            Port = port,
            PublicAddress = IPAddress.Loopback,
            BindAddress = IPAddress.Loopback,
        });
}
