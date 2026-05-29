using TorrentCs.Application;
using TorrentCs.Data;
using TorrentCs.Engine;
using TorrentCs.Modularity;
using TorrentCs.Modularity.MetainfoProvider;
using TorrentCs.Tracker;

namespace TorrentCs;

public sealed class TorrentDownload
{
    private readonly ManualResetEvent _completedEvent = new(false);

    // Metadata-fetch phase (BEP 9): present only when started from an info-hash.
    private readonly PipelineRunner? _metadataRunner;
    private readonly IApplicationProtocol? _partialProtocol;
    private readonly IMetainfoProvider? _metainfoProvider;
    private readonly Func<Metainfo, (PipelineRunner Runner, AggregatedTracker Tracker)>? _buildRunner;
    private readonly CancellationTokenSource _cts = new();

    private PipelineRunner? _runner;
    private AggregatedTracker _tracker;
    private Metainfo _description;
    private DownloadState _metadataState = DownloadState.FetchingMetadata;

    /// <summary>Creates a download for a torrent whose full metainfo is already known.</summary>
    public TorrentDownload(PipelineRunner runner, AggregatedTracker tracker)
    {
        _runner = runner;
        _tracker = tracker;
        _description = runner.Description;
        runner.DownloadCompleted += () => _completedEvent.Set();
    }

    private TorrentDownload(
        PipelineRunner metadataRunner,
        AggregatedTracker tracker,
        Metainfo partialMetainfo,
        IApplicationProtocol partialProtocol,
        IMetainfoProvider metainfoProvider,
        Func<Metainfo, (PipelineRunner, AggregatedTracker)> buildRunner)
    {
        _metadataRunner = metadataRunner;
        _tracker = tracker;
        _description = partialMetainfo;
        _partialProtocol = partialProtocol;
        _metainfoProvider = metainfoProvider;
        _buildRunner = buildRunner;
    }

    public Metainfo Description => _runner?.Description ?? _description;
    public DownloadState State => _runner?.State ?? _metadataState;
    public double Progress => _runner?.DownloadProgress ?? 0;

    /// <summary>Total bytes uploaded to peers so far.</summary>
    public long Uploaded => _runner?.Uploaded ?? 0;
    public IReadOnlyCollection<ITrackerDetails> Trackers => _tracker.Trackers;

    /// <summary>
    /// Creates a download that first fetches the metadata from peers (BEP 9) and then transitions to
    /// a normal download once the full metainfo is known.
    /// </summary>
    internal static TorrentDownload CreateForMetadata(
        PipelineRunner metadataRunner,
        AggregatedTracker tracker,
        Metainfo partialMetainfo,
        IApplicationProtocol partialProtocol,
        IMetainfoProvider metainfoProvider,
        Func<Metainfo, (PipelineRunner, AggregatedTracker)> buildRunner) =>
        new(metadataRunner, tracker, partialMetainfo, partialProtocol, metainfoProvider, buildRunner);

    public void Start()
    {
        if (_metadataRunner is null)
        {
            _runner!.Start();
            return;
        }

        _ = RunFromMetadataAsync();
    }

    public void Stop()
    {
        _cts.Cancel();
        _metadataRunner?.Stop();
        _runner?.Stop();
    }

    public Task WaitForDownloadCompletionAsync(TimeSpan? timeout = null)
    {
        return Task.Run(() =>
        {
            int ms = timeout.HasValue ? (int)timeout.Value.TotalMilliseconds : -1;
            if (!_completedEvent.WaitOne(ms))
                throw new TimeoutException("Download did not complete within the specified timeout.");
        });
    }

    public long DownloadRate() => _runner?.DownloadRateMeasurer.AverageRate() ?? 0;

    public long UploadRate() => _runner?.UploadRateMeasurer.AverageRate() ?? 0;

    // Runs the metadata-fetch phase, then builds and starts the normal download with the full metainfo.
    private async Task RunFromMetadataAsync()
    {
        try
        {
            _metadataRunner!.Start();
            var metainfo = await _metainfoProvider!.GetMetainfo((ITorrentContext)_partialProtocol!, _cts.Token);

            // Tear down the metadata-fetch phase completely (including its peer connections) before
            // starting the data download, so we reconnect cleanly rather than holding duplicate
            // connections to the same peers.
            _metadataRunner.Stop();
            _partialProtocol!.DisconnectAll();

            var (runner, tracker) = _buildRunner!(metainfo);
            _description = metainfo;
            _tracker = tracker;
            runner.DownloadCompleted += () => _completedEvent.Set();
            _runner = runner;
            runner.Start();
        }
        catch (OperationCanceledException)
        {
            // Stopped before the metadata could be fetched.
        }
        catch
        {
            _metadataState = DownloadState.Failed;
        }
    }
}
