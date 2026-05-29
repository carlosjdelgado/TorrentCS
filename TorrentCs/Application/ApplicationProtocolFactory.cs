using Microsoft.Extensions.Logging;
using TorrentCs.Application.BitTorrent;
using TorrentCs.Data;
using TorrentCs.Data.Pieces;
using TorrentCs.Modularity;

namespace TorrentCs.Application;

public class ApplicationProtocolFactory : IApplicationProtocolFactory
{
    private readonly ILoggerFactory _loggerFactory;
    private readonly IEnumerable<IModule> _modules;
    private readonly PeerId _localPeerId;

    public ApplicationProtocolFactory(
        ILoggerFactory loggerFactory,
        IEnumerable<IModule> modules,
        PeerId localPeerId)
    {
        _loggerFactory = loggerFactory;
        _modules = modules;
        _localPeerId = localPeerId;
    }

    public IApplicationProtocol Create(Metainfo metainfo, IBlockDataHandler dataHandler)
    {
        var pieceChecker = new PieceCheckerHandler(dataHandler, metainfo);
        var blockRequests = new BlockRequestManager();

        var protocol = new BitTorrentApplicationProtocol(
            _loggerFactory.CreateLogger<BitTorrentApplicationProtocol>(),
            metainfo,
            pieceChecker,
            blockRequests,
            _modules,
            _localPeerId);

        pieceChecker.PieceCompleted += protocol.PieceCompleted;
        pieceChecker.PieceCorrupted += protocol.PieceCorrupted;

        return protocol;
    }
}
