using System.Net;
using Microsoft.Extensions.Logging.Abstractions;
using TorrentCs.Tracker;
using TorrentCs.Tracker.Http;
using TorrentCs.Tracker.Udp;
using TorrentCs.Transport.Tcp;

namespace TorrentCs.Tests.Tracker;

public class TrackerClientFactoryTests
{
    private static TrackerClientFactory BuildFactory() =>
        new(NullLoggerFactory.Instance, new LocalTcpConnectionOptions
        {
            Port = 6881,
            PublicAddress = IPAddress.Loopback,
            BindAddress = IPAddress.Loopback,
        });

    [Fact]
    public void CreateTrackerClient_HttpUri_ReturnsHttpTracker()
    {
        var factory = BuildFactory();
        var tracker = factory.CreateTrackerClient(new Uri("http://tracker.example.com/announce"));
        Assert.IsType<HttpTracker>(tracker);
    }

    [Fact]
    public void CreateTrackerClient_HttpsUri_ReturnsHttpTracker()
    {
        var factory = BuildFactory();
        var tracker = factory.CreateTrackerClient(new Uri("https://tracker.example.com/announce"));
        Assert.IsType<HttpTracker>(tracker);
    }

    [Fact]
    public void CreateTrackerClient_UdpUri_ReturnsUdpTracker()
    {
        var factory = BuildFactory();
        var tracker = factory.CreateTrackerClient(new Uri("udp://tracker.example.com:6969"));
        Assert.IsType<UdpTracker>(tracker);
    }

    [Fact]
    public void CreateTrackerClient_UnknownScheme_ReturnsNull()
    {
        var factory = BuildFactory();
        var tracker = factory.CreateTrackerClient(new Uri("ftp://tracker.example.com"));
        Assert.Null(tracker);
    }

    [Fact]
    public void CreateTrackerClient_HttpTracker_HasCorrectType()
    {
        var factory = BuildFactory();
        var tracker = factory.CreateTrackerClient(new Uri("http://tracker.example.com/announce"));
        Assert.Equal("HTTP", tracker!.Type);
    }

    [Fact]
    public void CreateTrackerClient_UdpTracker_HasCorrectType()
    {
        var factory = BuildFactory();
        var tracker = factory.CreateTrackerClient(new Uri("udp://tracker.example.com:6969"));
        Assert.Equal("UDP", tracker!.Type);
    }
}
