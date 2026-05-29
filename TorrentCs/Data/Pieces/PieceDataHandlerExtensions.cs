namespace TorrentCs.Data.Pieces;

public static class PieceDataHandlerExtensions
{
    public static IEnumerable<Piece> IncompletePieces(this IPieceDataHandler handler)
        => handler.Metainfo.Pieces.Except(handler.CompletedPieces);
}
