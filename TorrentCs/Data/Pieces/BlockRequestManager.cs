namespace TorrentCs.Data.Pieces;

public class BlockRequestManager : IBlockRequests
{
    private readonly HashSet<Block> _requestedBlocks = new();
    private readonly HashSet<Block> _downloadedBlocks = new();

    public IReadOnlyCollection<Block> RequestedBlocks => _requestedBlocks;
    public IReadOnlyCollection<Block> DownloadedBlocks => _downloadedBlocks;

    public void BlockRequested(Block block) => _requestedBlocks.Add(block);

    public void BlockReceived(Block block)
    {
        _requestedBlocks.Remove(block);
        _downloadedBlocks.Add(block);
    }

    public void ClearBlocksForPiece(int pieceIndex)
    {
        _requestedBlocks.RemoveWhere(b => b.PieceIndex == pieceIndex);
        _downloadedBlocks.RemoveWhere(b => b.PieceIndex == pieceIndex);
    }
}
