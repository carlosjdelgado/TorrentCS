namespace TorrentCs.Data;

/// <summary>
/// The persisted state needed to resume a torrent without re-hashing its files: the info-hash it
/// belongs to, the total piece count, and which pieces have been verified.
/// </summary>
public sealed class ResumeData
{
    public ResumeData(Sha1Hash infoHash, int pieceCount, IEnumerable<int> completedPieces)
    {
        InfoHash = infoHash;
        PieceCount = pieceCount;
        CompletedPieces = completedPieces.ToList().AsReadOnly();
    }

    public Sha1Hash InfoHash { get; }
    public int PieceCount { get; }
    public IReadOnlyList<int> CompletedPieces { get; }
}
