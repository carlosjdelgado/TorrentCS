namespace TorrentCs.Data.Pieces;

public class BlockRequestManager : IBlockRequests
{
    private readonly Dictionary<Block, DateTime> _requestedBlocks = [];
    private readonly HashSet<Block> _downloadedBlocks = [];
    private readonly object _lock = new();

    // Snapshots: the download pipeline enumerates these on its own thread while the message-receive
    // thread mutates them, so callers must get an isolated copy taken under the lock.
    public IReadOnlyCollection<Block> RequestedBlocks
    {
        get { lock (_lock) return _requestedBlocks.Keys.ToList(); }
    }

    public IReadOnlyCollection<Block> DownloadedBlocks
    {
        get { lock (_lock) return _downloadedBlocks.ToList(); }
    }

    public void BlockRequested(Block block)
    {
        lock (_lock) _requestedBlocks[block] = DateTime.UtcNow;
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
            foreach (var block in _requestedBlocks.Keys.Where(b => b.PieceIndex == pieceIndex).ToList())
                _requestedBlocks.Remove(block);
            _downloadedBlocks.RemoveWhere(b => b.PieceIndex == pieceIndex);
        }
    }

    public void ExpireStaleRequests(TimeSpan timeout)
    {
        var cutoff = DateTime.UtcNow - timeout;
        lock (_lock)
        {
            foreach (var block in _requestedBlocks
                         .Where(kv => kv.Value < cutoff)
                         .Select(kv => kv.Key)
                         .ToList())
            {
                _requestedBlocks.Remove(block);
            }
        }
    }
}
