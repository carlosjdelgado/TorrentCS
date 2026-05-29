namespace TorrentCs.Tracker;

public class AggregatedTracker : ITracker
{
    private readonly ITrackerClientFactory _factory;
    private readonly IReadOnlyList<string> _trackerUrls;
    private readonly Dictionary<ITracker, TrackerStatistics> _active = new();

    public AggregatedTracker(ITrackerClientFactory factory, IEnumerable<string> trackerUrls)
    {
        _factory = factory;
        _trackerUrls = trackerUrls.ToList().AsReadOnly();
    }

    public string Type => "Aggregated";

    public IReadOnlyCollection<ITrackerDetails> Trackers =>
        _active.Values.Cast<ITrackerDetails>().ToList().AsReadOnly();

    public async Task<AnnounceResult> Announce(AnnounceRequest request)
    {
        EnsureCandidatesCreated();

        var tasks = _active.Select(async kv =>
        {
            kv.Value.LastAnnounce = DateTime.UtcNow;
            var result = await kv.Key.Announce(request);
            kv.Value.Peers = result.Peers.Count;
            return result;
        });

        var results = await Task.WhenAll(tasks);
        var allPeers = results.SelectMany(r => r.Peers);

        // Re-announce on the shortest interval any tracker asked for (floored to avoid hammering).
        var intervals = results
            .Where(r => r.Peers.Count > 0)
            .Select(r => r.Interval)
            .DefaultIfEmpty(AnnounceResult.DefaultInterval);
        int interval = Math.Max(60, intervals.Min());

        return new AnnounceResult(allPeers, interval);
    }

    private void EnsureCandidatesCreated()
    {
        if (_active.Count > 0) return;

        foreach (var (tracker, uri) in CreateCandidateTrackers())
            _active[tracker] = new TrackerStatistics(uri, tracker.Type);
    }

    private IEnumerable<(ITracker Tracker, Uri Uri)> CreateCandidateTrackers()
    {
        foreach (var url in _trackerUrls)
        {
            if (!Uri.TryCreate(url, UriKind.Absolute, out var uri)) continue;
            var tracker = _factory.CreateTrackerClient(uri);
            if (tracker is not null)
                yield return (tracker, uri);
        }
    }

    private sealed class TrackerStatistics : ITrackerDetails
    {
        public TrackerStatistics(Uri uri, string type)
        {
            Uri = uri;
            Type = type;
        }

        public Uri Uri { get; }
        public string Type { get; }
        public int Peers { get; set; }
        public DateTime LastAnnounce { get; set; }
    }
}
