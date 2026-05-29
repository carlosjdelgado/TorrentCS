namespace TorrentCs.Data;

/// <summary>A resume store that persists nothing — disables fast resume.</summary>
public sealed class NullResumeStore : IResumeStore
{
    public ResumeData? Load(string directory, Sha1Hash infoHash, int pieceCount) => null;

    public void Save(string directory, ResumeData data) { }
}
