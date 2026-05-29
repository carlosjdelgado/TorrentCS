using TorrentCs.Data;
using TorrentCs.Tracker;
using TorrentCs.Transport;

namespace TorrentCs.Tests.Tracker;

public class AggregatedTrackerTests
{
    private static AnnounceRequest MakeRequest() =>
        new(new byte[20], 0, 0, 0, new Sha1Hash(new byte[20]));

    [Fact]
    public void Type_IsAggregated()
    {
        var tracker = new AggregatedTracker(new StubFactory([]), []);
        Assert.Equal("Aggregated", tracker.Type);
    }

    [Fact]
    public async Task Announce_NoTrackers_ReturnsEmptyResult()
    {
        var tracker = new AggregatedTracker(new StubFactory([]), []);
        var result = await tracker.Announce(MakeRequest());
        Assert.Empty(result.Peers);
    }

    [Fact]
    public async Task Announce_SingleTracker_ReturnsPeers()
    {
        var peers = new[] { new FakeTransportStream(), new FakeTransportStream() };
        var factory = new StubFactory([new FakeTracker(peers)]);
        var tracker = new AggregatedTracker(factory, ["http://tracker.example.com"]);

        var result = await tracker.Announce(MakeRequest());
        Assert.Equal(2, result.Peers.Count);
    }

    [Fact]
    public async Task Announce_TrackerWithNoPeers_ReturnsEmpty()
    {
        var factory = new StubFactory([new FakeTracker([])]);
        var tracker = new AggregatedTracker(factory, ["http://tracker.example.com"]);

        var result = await tracker.Announce(MakeRequest());
        Assert.Empty(result.Peers);
    }

    [Fact]
    public async Task Trackers_AfterAnnounce_ContainsStatistics()
    {
        var factory = new StubFactory([new FakeTracker([new FakeTransportStream()])]);
        var tracker = new AggregatedTracker(factory, ["http://tracker.example.com"]);

        await tracker.Announce(MakeRequest());

        Assert.Single(tracker.Trackers);
    }

    [Fact]
    public async Task Announce_MultipleTrackers_AggregatesAllPeers()
    {
        var factory = new StubFactory(
        [
            new FakeTracker([new FakeTransportStream()]),
            new FakeTracker([new FakeTransportStream(), new FakeTransportStream()]),
        ]);
        var tracker = new AggregatedTracker(factory,
            ["http://t1.example.com", "udp://t2.example.com:6969"]);

        var result = await tracker.Announce(MakeRequest());

        Assert.Equal(3, result.Peers.Count);
        Assert.Equal(2, tracker.Trackers.Count);
    }

    [Fact]
    public async Task Announce_UsesShortestInterval_FlooredAt60()
    {
        var factory = new StubFactory(
        [
            new FakeTracker([new FakeTransportStream()], interval: 900),
            new FakeTracker([new FakeTransportStream()], interval: 300),
        ]);
        var tracker = new AggregatedTracker(factory,
            ["http://t1.example.com", "http://t2.example.com"]);

        var result = await tracker.Announce(MakeRequest());

        Assert.Equal(300, result.Interval);
    }

    // ─── Test doubles ────────────────────────────────────────────────────────

    private sealed class StubFactory(IList<ITracker> trackers) : ITrackerClientFactory
    {
        private int _index;
        public ITracker? CreateTrackerClient(Uri uri) =>
            _index < trackers.Count ? trackers[_index++] : null;
    }

    private sealed class FakeTracker(IEnumerable<ITransportStream> peers, int interval = AnnounceResult.DefaultInterval)
        : ITracker
    {
        public string Type => "Fake";
        public Task<AnnounceResult> Announce(AnnounceRequest request) =>
            Task.FromResult(new AnnounceResult(peers, interval));
    }

    private sealed class FakeTransportStream : ITransportStream
    {
        public bool IsConnected => false;
        public string DisplayAddress => "fake";
        public object Address => "fake";
        public Stream Stream => Stream.Null;
        public Task Connect() => Task.CompletedTask;
        public void Disconnect() { }
    }
}
