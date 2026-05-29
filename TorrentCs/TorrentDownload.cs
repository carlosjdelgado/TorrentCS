using TorrentCs.Data;
using TorrentCs.Engine;
using TorrentCs.Tracker;

namespace TorrentCs;

public sealed class TorrentDownload
{
    private readonly PipelineRunner _runner;
    private readonly AggregatedTracker _tracker;
    private readonly ManualResetEvent _completedEvent = new(false);

    public TorrentDownload(PipelineRunner runner, AggregatedTracker tracker)
    {
        _runner = runner;
        _tracker = tracker;
        _runner.DownloadCompleted += () => _completedEvent.Set();
    }

    public Metainfo Description => _runner.Description;
    public DownloadState State => _runner.State;
    public double Progress => _runner.DownloadProgress;
    public IReadOnlyCollection<ITrackerDetails> Trackers => _tracker.Trackers;

    public void Start() => _runner.Start();

    public void Stop() => _runner.Stop();

    public Task WaitForDownloadCompletionAsync(TimeSpan? timeout = null)
    {
        return Task.Run(() =>
        {
            int ms = timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : -1;
            if (!_completedEvent.WaitOne(ms))
                throw new TimeoutException("Download did not complete within the specified timeout.");
        });
    }

    public long DownloadRate() => _runner.DownloadRateMeasurer.AverageRate();

    public long UploadRate() => _runner.UploadRateMeasurer.AverageRate();
}
