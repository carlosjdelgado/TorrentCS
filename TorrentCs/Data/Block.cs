namespace TorrentCs.Data;

public class Block
{
    public Block(int pieceIndex, int offset, byte[] data)
    {
        PieceIndex = pieceIndex;
        Offset = offset;
        Data = data;
    }

    public int PieceIndex { get; }
    public int Offset { get; }
    public byte[] Data { get; }
    public int Length => Data.Length;

    public override bool Equals(object? obj) =>
        obj is Block other && PieceIndex == other.PieceIndex && Offset == other.Offset;

    public override int GetHashCode() => HashCode.Combine(PieceIndex, Offset);
}
