using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;

namespace TorrentCs.Transport.Tcp;

public class TcpTransportProtocol : ITcpTransportProtocol
{
    private readonly ILogger<TcpTransportProtocol> _logger;
    private readonly LocalTcpConnectionOptions _options;
    private readonly List<ITransportStream> _streams = new();
    private TcpListener? _listener;
    private CancellationTokenSource? _cts;

    public TcpTransportProtocol(ILogger<TcpTransportProtocol> logger, LocalTcpConnectionOptions options)
    {
        _logger = logger;
        _options = options;
    }

    public event Action<AcceptConnectionEventArgs>? AcceptConnectionHandler;

    public IReadOnlyCollection<ITransportStream> Streams => _streams.AsReadOnly();
    public int Port { get; private set; }
    public RateLimiter RateLimiter { get; } = new();

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _listener = new TcpListener(_options.BindAddress, _options.Port);
        _listener.Start();
        Port = ((IPEndPoint)_listener.LocalEndpoint).Port;
        _ = ListenForIncomingConnectionsAsync(_cts.Token);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _listener?.Stop();
        foreach (var stream in _streams.ToList())
            stream.Disconnect();
        _streams.Clear();
    }

    public TcpTransportStream CreateTransportStream(IPAddress remoteAddress, int port)
        => new(_options.BindAddress, remoteAddress, port);

    private async Task ListenForIncomingConnectionsAsync(CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                var client = await _listener!.AcceptTcpClientAsync(ct);
                AcceptConnection(client);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error accepting incoming TCP connection");
                break;
            }
        }
    }

    private void AcceptConnection(TcpClient client)
    {
        var stream = new TcpTransportStream(client);
        bool accepted = false;
        var args = new AcceptConnectionEventArgs(stream, () => accepted = true);
        AcceptConnectionHandler?.Invoke(args);

        if (accepted)
            _streams.Add(stream);
        else
            stream.Disconnect();
    }
}
