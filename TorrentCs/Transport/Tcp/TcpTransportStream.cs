using System.Net;
using System.Net.Sockets;

namespace TorrentCs.Transport.Tcp;

public class TcpTransportStream : ITransportStream
{
    private static readonly TimeSpan ConnectTimeout = TimeSpan.FromSeconds(8);

    private readonly IPAddress? _remoteAddress;
    private readonly int _port;
    private readonly TcpClient _client;
    private readonly SemaphoreSlim _connectGuard = new(1, 1);

    public TcpTransportStream(IPAddress bindAddress, IPAddress remoteAddress, int port)
    {
        _remoteAddress = remoteAddress;
        _port = port;
        // Bind to a specific local adapter only when one is requested; otherwise let the
        // OS pick a route and address family that match the remote peer (IPv4 or IPv6).
        _client = bindAddress.Equals(IPAddress.Any)
            ? new TcpClient(remoteAddress.AddressFamily)
            : new TcpClient(new IPEndPoint(bindAddress, 0));
        // The target endpoint is known up front, so peers can be identified (and deduplicated)
        // before the connection is established.
        RemoteEndPoint = new IPEndPoint(remoteAddress, port);
        Stream = Stream.Null;
    }

    public TcpTransportStream(TcpClient client)
    {
        _client = client;
        RemoteEndPoint = (IPEndPoint)client.Client.RemoteEndPoint!;
        Stream = new RateLimitedStream(client.GetStream(), new RateLimiter());
    }

    public IPEndPoint? RemoteEndPoint { get; private set; }
    public bool IsConnected => _client.Connected;
    public bool IsConnecting { get; private set; }
    public Stream Stream { get; private set; }
    public string DisplayAddress => RemoteEndPoint?.ToString() ?? "not connected";
    public object Address => RemoteEndPoint ?? (object)"not connected";

    public async Task Connect()
    {
        await _connectGuard.WaitAsync();
        try
        {
            if (IsConnected || IsConnecting) return;
            IsConnecting = true;
            // Time out unresponsive peers (filtered / behind NAT / dead) so they don't hold a
            // connection slot indefinitely while hundreds of other peers go untried.
            using var cts = new CancellationTokenSource(ConnectTimeout);
            await _client.ConnectAsync(_remoteAddress!, _port, cts.Token);
            RemoteEndPoint = (IPEndPoint)_client.Client.RemoteEndPoint!;
            Stream = new RateLimitedStream(_client.GetStream(), new RateLimiter());
        }
        finally
        {
            IsConnecting = false;
            _connectGuard.Release();
        }
    }

    public void Disconnect()
    {
        _client.Close();
        _connectGuard.Dispose();
    }
}
