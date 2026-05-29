using Microsoft.Extensions.Logging;
using TorrentCs.Tracker.Http;
using TorrentCs.Tracker.Udp;
using TorrentCs.Transport.Tcp;

namespace TorrentCs.Tracker;

public class TrackerClientFactory : ITrackerClientFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly LocalTcpConnectionOptions _options;

    public TrackerClientFactory(ILoggerFactory loggerFactory, LocalTcpConnectionOptions options)
    {
        _loggerFactory = loggerFactory;
        _options = options;
    }

    public ITracker? CreateTrackerClient(Uri trackerUri) => trackerUri.Scheme.ToLower() switch
    {
        "http" or "https" => new HttpTracker(
            _loggerFactory.CreateLogger<HttpTracker>(), _options, trackerUri),

        "udp" => new UdpTracker(
            _loggerFactory.CreateLogger<UdpTracker>(), _options, trackerUri),

        _ => null,
    };
}
