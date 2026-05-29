namespace TorrentCs.Data;

public class Metainfo
{
    public Metainfo(
        string name,
        Sha1Hash infoHash,
        IEnumerable<ContainedFile> files,
        IEnumerable<Piece> pieces,
        int pieceSize,
        IEnumerable<string> trackers,
        byte[] rawInfoDict)
    {
        Name = name;
        InfoHash = infoHash;
        Files = files.ToList().AsReadOnly();
        Pieces = pieces.ToList().AsReadOnly();
        PieceSize = pieceSize;
        Trackers = trackers.ToList().AsReadOnly();
        RawInfoDict = rawInfoDict;
    }

    /// <summary>
    /// Creates a partial metainfo holding only the info-hash and trackers, for starting a download
    /// from a magnet link / info-hash before the metadata has been fetched (BEP 9). It is not usable
    /// for downloading data; <see cref="IsComplete"/> is false until the metadata is obtained.
    /// </summary>
    public static Metainfo Partial(Sha1Hash infoHash, IEnumerable<string> trackers) =>
        new(string.Empty, infoHash, [], [], 0, trackers, []);

    public string Name { get; }
    public Sha1Hash InfoHash { get; }
    public IReadOnlyList<ContainedFile> Files { get; }
    public IReadOnlyList<Piece> Pieces { get; }
    public int PieceSize { get; }
    public IReadOnlyList<string> Trackers { get; }
    public byte[] RawInfoDict { get; }

    /// <summary>Whether the full metadata (info dictionary) is present, i.e. data can be downloaded.</summary>
    public bool IsComplete => RawInfoDict.Length > 0;

    public long PieceOffset(Piece piece) => (long)piece.Index * PieceSize;

    public int FileIndex(long dataOffset)
    {
        long position = 0;
        for (int i = 0; i < Files.Count; i++)
        {
            position += Files[i].Size;
            if (dataOffset < position) return i;
        }
        return Files.Count - 1;
    }

    public long FileOffset(int fileIndex)
    {
        long offset = 0;
        for (int i = 0; i < fileIndex; i++)
            offset += Files[i].Size;
        return offset;
    }
}
