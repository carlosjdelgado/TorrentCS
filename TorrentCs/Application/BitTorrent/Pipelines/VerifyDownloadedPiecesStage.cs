using System.Security.Cryptography;
using Microsoft.Extensions.Logging;
using TorrentCs.Application.Pipelines;
using TorrentCs.Data;

namespace TorrentCs.Application.BitTorrent.Pipelines;

public class VerifyDownloadedPiecesStage : IPipelineStage
{
    private readonly ILogger<VerifyDownloadedPiecesStage> _logger;
    private readonly IApplicationProtocol _protocol;

    public VerifyDownloadedPiecesStage(ILogger<VerifyDownloadedPiecesStage> logger, IApplicationProtocol protocol)
    {
        _logger = logger;
        _protocol = protocol;
    }

    public void Run(IStageInterrupt interrupt, IProgress<StatusUpdate> progress)
    {
        var incomplete = _protocol.DataHandler.Metainfo.Pieces
            .Except(_protocol.DataHandler.CompletedPieces).ToList();

        if (incomplete.Count == 0)
        {
            _logger.LogDebug("No pieces to verify — starting fresh");
            return;
        }

        HashAndMarkPieces(interrupt, progress);
    }

    private void HashAndMarkPieces(IStageInterrupt interrupt, IProgress<StatusUpdate> progress)
    {
        var metainfo = _protocol.DataHandler.Metainfo;
        int total = metainfo.Pieces.Count;
        int verified = 0;

        foreach (var piece in metainfo.Pieces)
        {
            if (interrupt.IsStopRequested) return;

            while (interrupt.IsPauseRequested)
                Thread.Sleep(50);

            long offset = metainfo.PieceOffset(piece);
            if (_protocol.DataHandler.TryReadBlockData(offset, piece.Size, out var data))
            {
                var hash = new Sha1Hash(SHA1.HashData(data));
                if (hash == piece.Hash)
                    _protocol.DataHandler.MarkPieceAsCompleted(piece);
            }

            verified++;
            progress.Report(new StatusUpdate(DownloadState.Downloading, (double)verified / total));
        }

        int completed = _protocol.DataHandler.CompletedPieces.Count;
        _logger.LogInformation("{Completed}/{Total} pieces already downloaded", completed, total);
    }
}
