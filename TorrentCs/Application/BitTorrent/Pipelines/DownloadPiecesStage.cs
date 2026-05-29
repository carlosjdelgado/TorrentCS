using Microsoft.Extensions.Logging;
using TorrentCs.Application.BitTorrent.Messages;
using TorrentCs.Application.Pipelines;
using TorrentCs.Data;
using TorrentCs.Data.Pieces;
using TorrentCs.Modularity;
using TorrentCs.Transport;

namespace TorrentCs.Application.BitTorrent.Pipelines;

public class DownloadPiecesStage : IPipelineStage
{
    private const int MaxConnectedPeers = 50;
    private const int IterationDelayMs = 100;
    private static readonly TimeSpan BlockRequestTimeout = TimeSpan.FromSeconds(30);

    private readonly ILogger<DownloadPiecesStage> _logger;
    private readonly IApplicationProtocol _protocol;
    private readonly IPiecePicker _piecePicker;

    public DownloadPiecesStage(
        ILogger<DownloadPiecesStage> logger,
        IApplicationProtocol protocol,
        IPiecePicker piecePicker)
    {
        _logger = logger;
        _protocol = protocol;
        _piecePicker = piecePicker;
    }

    public void Run(IStageInterrupt interrupt, IProgress<StatusUpdate> progress)
    {
        while (!interrupt.IsStopRequested)
        {
            if (!interrupt.IsPauseRequested)
            {
                // A transient error with one peer (e.g. a dropped connection) must not abort the
                // whole download; log it and carry on with the next iteration.
                try { Iterate(progress); }
                catch (Exception ex) { _logger.LogWarning("Download iteration error: {Reason}", ex.Message); }
            }

            interrupt.InterruptHandle.WaitOne(IterationDelayMs);
        }
    }

    private void Iterate(IProgress<StatusUpdate> progress)
    {
        int completedCount = _protocol.DataHandler.CompletedPieces.Count;
        int totalCount = _protocol.DataHandler.Metainfo.Pieces.Count;
        double prog = totalCount == 0 ? 0 : (double)completedCount / totalCount;
        progress.Report(new StatusUpdate(DownloadState.Downloading, prog));

        var peers = _protocol.Peers.OfType<BitTorrentPeer>().ToList();
        RequestPieces(peers);
        ConnectToPeers();
    }

    private void RequestPieces(List<BitTorrentPeer> peers)
    {
        if (peers.Count == 0) return;

        // Drop requests a peer never answered so they can be asked again — avoids stalling when a
        // peer goes silent after we've requested blocks from it.
        _protocol.BlockRequests.ExpireStaleRequests(BlockRequestTimeout);

        var availability = new Bitfield(_protocol.DataHandler.Metainfo.Pieces.Count);
        foreach (var peer in peers)
            availability.Union(peer.Available);

        var incomplete = _protocol.DataHandler.IncompletePieces().ToList();
        var requested = _protocol.BlockRequests.RequestedBlocks
            .Concat(_protocol.BlockRequests.DownloadedBlocks)
            .Select(b => new BlockRequest(b.PieceIndex, b.Offset, b.Length))
            .ToList();

        var peerList = peers.Cast<IPeer>().ToList();
        var toRequest = _piecePicker.BlocksToRequest(incomplete, availability, peerList, requested);

        foreach (var req in toRequest)
        {
            var peer = peers.FirstOrDefault(p =>
                p.Available.IsPieceAvailable(req.PieceIndex) && !p.IsChokedByRemotePeer);
            if (peer is null) continue;

            _protocol.BlockRequests.BlockRequested(
                new Block(req.PieceIndex, req.Offset, new byte[req.Length]));

            using var ms = new MemoryStream(12);
            var be = new BigEndianBinaryWriter(ms);
            be.Write(req.PieceIndex);
            be.Write(req.Offset);
            be.Write(req.Length);
            peer.SendMessage(RequestMessage.MessageID, ms.ToArray());
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
