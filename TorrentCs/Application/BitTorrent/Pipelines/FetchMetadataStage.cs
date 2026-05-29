using Microsoft.Extensions.Logging;
using TorrentCs.Application.Pipelines;

namespace TorrentCs.Application.BitTorrent.Pipelines;

/// <summary>
/// Pipeline stage for the BEP 9 metadata-fetch phase: it only keeps connections to peers open so
/// they can serve us the metadata via ut_metadata. The actual reassembly happens in the metadata
/// extension; this stage runs until stopped (the orchestrator stops it once the metadata arrives).
/// </summary>
public class FetchMetadataStage : IPipelineStage
{
    private const int MaxConnectedPeers = 5;
    private const int IterationDelayMs = 200;

    private readonly ILogger<FetchMetadataStage> _logger;
    private readonly IApplicationProtocol _protocol;

    public FetchMetadataStage(ILogger<FetchMetadataStage> logger, IApplicationProtocol protocol)
    {
        _logger = logger;
        _protocol = protocol;
    }

    public void Run(IStageInterrupt interrupt, IProgress<StatusUpdate> progress)
    {
        _logger.LogDebug("Fetching metadata from peers for {InfoHash}", _protocol.Metainfo.InfoHash);
        progress.Report(new StatusUpdate(DownloadState.FetchingMetadata, 0));

        while (!interrupt.IsStopRequested)
        {
            if (!interrupt.IsPauseRequested)
                ConnectToPeers();
            interrupt.InterruptHandle.WaitOne(IterationDelayMs);
        }
    }

    private void ConnectToPeers()
    {
        int current = _protocol.Peers.Count + _protocol.ConnectingPeers.Count;
        if (current >= MaxConnectedPeers) return;

        foreach (var peer in _protocol.AvailablePeers.Take(MaxConnectedPeers - current))
            _protocol.ConnectToPeer(peer);
    }
}
