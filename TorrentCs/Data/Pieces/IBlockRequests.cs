namespace TorrentCs.Data.Pieces;

public interface IBlockRequests
{
    IReadOnlyCollection<Block> RequestedBlocks { get; }
    IReadOnlyCollection<Block> DownloadedBlocks { get; }

    void BlockRequested(Block block);
    void BlockReceived(Block block);
    void ClearBlocksForPiece(int pieceIndex);

    /// <summary>
    /// Drops outstanding requests older than <paramref name="timeout"/> so they can be requested
    /// again (e.g. from another peer) — prevents stalls when a peer goes silent after being asked.
    /// </summary>
    void ExpireStaleRequests(TimeSpan timeout);
}
