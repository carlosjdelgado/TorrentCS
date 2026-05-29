namespace TorrentCs.Data;

/// <summary>
/// Persists and restores per-torrent resume state, keyed by info-hash, in a given directory.
/// </summary>
public interface IResumeStore
{
    /// <summary>
    /// Loads resume state for a torrent, or null if none exists or it does not match the torrent
    /// (different info-hash or piece count).
    /// </summary>
    ResumeData? Load(string directory, Sha1Hash infoHash, int pieceCount);

    void Save(string directory, ResumeData data);
}
