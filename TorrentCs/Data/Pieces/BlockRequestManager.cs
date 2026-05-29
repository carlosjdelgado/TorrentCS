namespace TorrentCs.Data.Pieces;

public class BlockRequestManager : IBlockRequests
{
    private readonly HashSet<Block> _requestedBlocks = [];
    private readonly HashSet<Block> _downloadedBlocks = [];
    private readonly object _lock = new();

    // Snapshots: the download pipeline enumerates these on its own thread while the message-receive
    // thread mutates them, so callers must get an isolated copy taken under the lock.
    public IReadOnlyCollection<Block> RequestedBlocks
    {
        get { lock (_lock) return _requestedBlocks.ToList(); }
    }

    public IReadOnlyCollection<Block> DownloadedBlocks
    {
        get { lock (_lock) return _downloadedBlocks.ToList(); }
    }

    public void BlockRequested(Block block)
    {
        lock (_lock) _requestedBlocks.Add(block);
    }

    public void BlockReceived(Block block)
    {
        lock (_lock)
        {
            _requestedBlocks.Remove(block);
            _downloadedBlocks.Add(block);
        }
    }

    public void ClearBlocksForPiece(int pieceIndex)
    {
        lock (_lock)
        {
            _requestedBlocks.RemoveWhere(b => b.PieceIndex == pieceIndex);
            _downloadedBlocks.RemoveWhere(b => b.PieceIndex == pieceIndex);
        }
    }
}
