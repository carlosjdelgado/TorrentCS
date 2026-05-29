using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using TorrentCs.Transport;
using TorrentCs.Transport.Tcp;

namespace TorrentCs.Tracker.Udp;

public class UdpTracker : ITracker
{
    private const long ConnectionProtocolId = 0x41727101980L;
    private static readonly TimeSpan ReceiveTimeout = TimeSpan.FromSeconds(15);

    private readonly ILogger<UdpTracker> _logger;
    private readonly LocalTcpConnectionOptions _options;
    private readonly Uri _trackerUri;
    private readonly Random _random = new();

    public UdpTracker(ILogger<UdpTracker> logger, LocalTcpConnectionOptions options, Uri trackerUri)
    {
        _logger = logger;
        _options = options;
        _trackerUri = trackerUri;
    }

    public string Type => "UDP";

    public async Task<AnnounceResult> Announce(AnnounceRequest request)
    {
        _logger.LogDebug("UDP announce to {Uri}", _trackerUri);

        try
        {
            using var udp = new UdpClient(0);
            udp.Connect(_trackerUri.Host, _trackerUri.Port);

            var connectionId = await ConnectAsync(udp);
            var (peers, interval) = await AnnounceAsync(udp, connectionId, request);

            _logger.LogDebug("Tracker {Uri} returned {Count} peers (interval {Interval}s)",
                _trackerUri, peers.Count(), interval);
            return new AnnounceResult(peers, interval);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "UDP announce failed for {Uri}", _trackerUri);
            return new AnnounceResult([]);
        }
    }

    private async Task<long> ConnectAsync(UdpClient udp)
    {
        var req = new ConnectionRequestMessage
        {
            ConnectionId = ConnectionProtocolId,
            TransactionId = _random.Next(),
        };

        var resp = await SendAndReceiveAsync<ConnectionResponseMessage>(udp, req);

        if (resp.TransactionId != req.TransactionId)
            throw new InvalidDataException("Transaction ID mismatch in connect response.");

        return resp.ConnectionId;
    }

    private async Task<(IEnumerable<TcpTransportStream> Peers, int Interval)> AnnounceAsync(
        UdpClient udp, long connectionId, AnnounceRequest request)
    {
        var req = new AnnounceRequestMessage
        {
            ConnectionId = connectionId,
            TransactionId = _random.Next(),
            InfoHash = request.InfoHash,
            PeerId = request.PeerId,
            Downloaded = request.Downloaded,
            LeftToDownload = request.Remaining,
            Uploaded = request.Uploaded,
            Event = MapEvent(request.Event),
            NumWant = request.NumWant,
            Port = (ushort)_options.Port,
        };

        var resp = await SendAndReceiveAsync<AnnounceResponseMessage>(udp, req);

        if (resp.TransactionId != req.TransactionId)
            throw new InvalidDataException("Transaction ID mismatch in announce response.");

        var peers = resp.Peers.Select(p =>
            new TcpTransportStream(_options.BindAddress, p.IPAddress, p.Port));
        return (peers, resp.Interval > 0 ? resp.Interval : AnnounceResult.DefaultInterval);
    }

    private static AnnounceRequestMessage.EventType MapEvent(TrackerEvent e) => e switch
    {
        TrackerEvent.Started => AnnounceRequestMessage.EventType.Started,
        TrackerEvent.Stopped => AnnounceRequestMessage.EventType.Stopped,
        TrackerEvent.Completed => AnnounceRequestMessage.EventType.Completed,
        _ => AnnounceRequestMessage.EventType.None,
    };

    private async Task<T> SendAndReceiveAsync<T>(UdpClient udp, UdpTrackerRequestMessage request)
        where T : UdpTrackerResponseMessage, new()
    {
        Send(udp, request);

        using var cts = new CancellationTokenSource(ReceiveTimeout);
        var result = await udp.ReceiveAsync(cts.Token);

        return Receive<T>(result.Buffer);
    }

    private static void Send(UdpClient udp, UdpTrackerRequestMessage message)
    {
        using var ms = new MemoryStream();
        using var writer = new BinaryWriter(ms);
        message.WriteTo(writer);
        writer.Flush();
        var data = ms.ToArray();
        udp.Send(data, data.Length);
    }

    private static T Receive<T>(byte[] buffer) where T : UdpTrackerResponseMessage, new()
    {
        using var ms = new MemoryStream(buffer);
        using var reader = new BinaryReader(ms);
        var message = new T();
        message.ReadFrom(reader);
        return message;
    }
}
