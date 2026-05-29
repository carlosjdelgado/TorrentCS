namespace TorrentCs.Data;

public interface IPieceCalculator
{
    void ComputePieces(List<ContainedFile> files, int pieceSize, IFileHandler fileHandler, List<Piece> pieces);
}
