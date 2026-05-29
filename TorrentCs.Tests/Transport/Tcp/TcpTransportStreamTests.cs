using System.Net;
using System.Net.Sockets;
using TorrentCs.Transport;
using TorrentCs.Transport.Tcp;

namespace TorrentCs.Tests.Transport.Tcp;

public class TcpTransportStreamTests : IAsyncLifetime
{
    private TcpListener _server = null!;
    private int _serverPort;

    public Task InitializeAsync()
    {
        _server = new TcpListener(IPAddress.Loopback, 0);
        _server.Start();
        _serverPort = ((IPEndPoint)_server.LocalEndpoint).Port;
        return Task.CompletedTask;
    }

    public Task DisposeAsync()
    {
        _server.Stop();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task Connect_EstablishesConnection()
    {
        var stream = new TcpTransportStream(IPAddress.Loopback, IPAddress.Loopback, _serverPort);
        var acceptTask = _server.AcceptTcpClientAsync();

        await stream.Connect();
        (await acceptTask).Dispose();

        Assert.True(stream.IsConnected);
        stream.Disconnect();
    }

    [Fact]
    public async Task Connect_SetsRemoteEndPoint()
    {
        var stream = new TcpTransportStream(IPAddress.Loopback, IPAddress.Loopback, _serverPort);
        var acceptTask = _server.AcceptTcpClientAsync();

        await stream.Connect();
        (await acceptTask).Dispose();

        Assert.NotNull(stream.RemoteEndPoint);
        Assert.Equal(_serverPort, stream.RemoteEndPoint!.Port);
        stream.Disconnect();
    }

    [Fact]
    public async Task Connect_SetsStreamToRateLimitedStream()
    {
        var stream = new TcpTransportStream(IPAddress.Loopback, IPAddress.Loopback, _serverPort);
        var acceptTask = _server.AcceptTcpClientAsync();

        await stream.Connect();
        (await acceptTask).Dispose();

        Assert.IsType<RateLimitedStream>(stream.Stream);
        stream.Disconnect();
    }

    [Fact]
    public async Task IncomingConnection_IsConnected()
    {
        var (transportStream, client) = await AcceptLoopbackAsync();
        client.Dispose();

        Assert.True(transportStream.IsConnected);
        transportStream.Disconnect();
    }

    [Fact]
    public async Task IncomingConnection_StreamIsRateLimited()
    {
        var (transportStream, client) = await AcceptLoopbackAsync();
        client.Dispose();

        Assert.IsType<RateLimitedStream>(transportStream.Stream);
        transportStream.Disconnect();
    }

    [Fact]
    public async Task IncomingConnection_DisplayAddressIsNotEmpty()
    {
        var (transportStream, client) = await AcceptLoopbackAsync();
        client.Dispose();

        Assert.NotEmpty(transportStream.DisplayAddress);
        transportStream.Disconnect();
    }

    [Fact]
    public async Task Disconnect_ClosesConnection()
    {
        var (transportStream, client) = await AcceptLoopbackAsync();
        client.Dispose();

        transportStream.Disconnect();

        Assert.False(transportStream.IsConnected);
    }

    private async Task<(TcpTransportStream, TcpClient)> AcceptLoopbackAsync()
    {
        using var local = new TcpListener(IPAddress.Loopback, 0);
        local.Start();
        int port = ((IPEndPoint)local.LocalEndpoint).Port;

        var client = new TcpClient();
        var connectTask = client.ConnectAsync(IPAddress.Loopback, port);
        var accepted = await local.AcceptTcpClientAsync();
        await connectTask;
        local.Stop();

        return (new TcpTransportStream(accepted), client);
    }
}
