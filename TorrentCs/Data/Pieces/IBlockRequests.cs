namespace TorrentCs.Data.Pieces;

public interface IBlockRequests
{
    IReadOnlyCollection<Block> RequestedBlocks { get; }
    IReadOnlyCollection<Block> DownloadedBlocks { get; }

    void BlockRequested(Block block);
    void BlockReceived(Block block);
    void ClearBlocksForPiece(int pieceIndex);
}
