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

    public string Name { get; }
    public Sha1Hash InfoHash { get; }
    public IReadOnlyList<ContainedFile> Files { get; }
    public IReadOnlyList<Piece> Pieces { get; }
    public int PieceSize { get; }
    public IReadOnlyList<string> Trackers { get; }
    public byte[] RawInfoDict { get; }

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
