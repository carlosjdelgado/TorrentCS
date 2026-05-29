using TorrentCs.Application;
using TorrentCs.Application.BitTorrent;
using TorrentCs.Application.Pipelines;
using TorrentCs.Data;
using TorrentCs.Tracker;

namespace TorrentCs.Engine;

public class PipelineRunner : ITorrentPipelineRunner
{
    private readonly IApplicationProtocol _protocol;
    private readonly ITracker _tracker;
    private readonly IPipeline _pipeline;
    private readonly IMainLoop _mainLoop;
    private readonly PeerId _localPeerId;
    private readonly StageInterrupt _interrupt = new();
    private readonly CancellationTokenSource _trackerCts = new();
    private IRegularTask? _statisticsTask;
    private long _lastDownloaded;
    private long _lastUploaded;

    public PipelineRunner(
        IApplicationProtocol protocol,
        ITracker tracker,
        IPipeline pipeline,
        IMainLoop mainLoop,
        PeerId localPeerId)
    {
        _protocol = protocol;
        _tracker = tracker;
        _pipeline = pipeline;
        _mainLoop = mainLoop;
        _localPeerId = localPeerId;

        _protocol.DownloadCompleted += () =>
        {
            State = DownloadState.Completed;
            DownloadCompleted?.Invoke();
        };
    }

    public event Action? DownloadCompleted;

    public Metainfo Description => _protocol.Metainfo;
    public DownloadState State { get; private set; } = DownloadState.Downloading;
    public RateMeasurer DownloadRateMeasurer { get; } = new();
    public RateMeasurer UploadRateMeasurer { get; } = new();

    public long Downloaded => _protocol.DataHandler.CompletedPieces
        .Sum(p => (long)p.Size);

    public long Uploaded => _protocol.Uploaded;

    public double DownloadProgress
    {
        get
        {
            long total = _protocol.Metainfo.Pieces.Sum(p => (long)p.Size);
            return total == 0 ? 0 : (double)Downloaded / total;
        }
    }

    public void Start()
    {
        _statisticsTask = _mainLoop.AddRegularTask(UpdateStatistics);
        _ = TrackerLoopAsync(_trackerCts.Token);
        _ = Task.Run(() =>
        {
            var progress = new Progress<StatusUpdate>(u =>
            {
                if (u.State == DownloadState.Failed) State = DownloadState.Failed;
            });
            _pipeline.Run(_interrupt, progress);
        });
    }

    public void Pause() => _interrupt.Pause();

    public void Stop()
    {
        _interrupt.Stop();
        _trackerCts.Cancel();
        _statisticsTask?.Dispose();
    }

    private async Task TrackerLoopAsync(CancellationToken ct)
    {
        bool first = true;
        while (!ct.IsCancellationRequested)
        {
            int interval = await ContactTrackerAsync(first ? TrackerEvent.Started : TrackerEvent.None);
            first = false;
            try
            {
                await Task.Delay(TimeSpan.FromSeconds(Math.Max(60, interval)), ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
        }
    }

    private async Task<int> ContactTrackerAsync(TrackerEvent @event)
    {
        try
        {
            var request = new AnnounceRequest(
                _localPeerId.Value,
                remaining: Description.Pieces.Sum(p => (long)p.Size) - Downloaded,
                downloaded: Downloaded,
                uploaded: Uploaded,
                Description.InfoHash,
                @event);

            var result = await _tracker.Announce(request);
            _mainLoop.AddTask(() => _protocol.PeersAvailable(result.Peers));
            return result.Interval;
        }
        catch
        {
            return AnnounceResult.DefaultInterval; // tracker errors are non-fatal
        }
    }

    private void UpdateStatistics()
    {
        // RateMeasurer expects the bytes transferred since the last sample, not the running total.
        long downloaded = Downloaded;
        long uploaded = Uploaded;
        DownloadRateMeasurer.AddMeasure(downloaded - _lastDownloaded);
        UploadRateMeasurer.AddMeasure(uploaded - _lastUploaded);
        _lastDownloaded = downloaded;
        _lastUploaded = uploaded;
    }
}
