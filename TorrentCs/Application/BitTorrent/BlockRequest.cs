namespace TorrentCs.Application.BitTorrent;

public class BlockRequest
{
    public BlockRequest(int pieceIndex, int offset, int length)
    {
        PieceIndex = pieceIndex;
        Offset = offset;
        Length = length;
    }

    public int PieceIndex { get; }
    public int Offset { get; }
    public int Length { get; }

    public static bool operator ==(BlockRequest? a, BlockRequest? b)
    {
        if (a is null) return b is null;
        return a.PieceIndex == b?.PieceIndex && a.Offset == b.Offset && a.Length == b.Length;
    }

    public static bool operator !=(BlockRequest? a, BlockRequest? b) => !(a == b);

    public override bool Equals(object? obj) => obj is BlockRequest other && this == other;

    public override int GetHashCode() =>
        PieceIndex * 7 ^ Offset * 7 ^ Length * 7;
}
