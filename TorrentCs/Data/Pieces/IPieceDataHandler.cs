namespace TorrentCs.Data.Pieces;

public interface IPieceDataHandler : IBlockDataHandler
{
    event Action<Piece>? PieceCorrupted;
    event Action<Piece>? PieceCompleted;

    IReadOnlyCollection<Piece> CompletedPieces { get; }

    void MarkPieceAsCompleted(Piece piece);
}
