namespace TorrentCs.Application.BitTorrent;

public class Bitfield
{
    private readonly byte[] _data;

    public Bitfield(int pieceCount)
    {
        PieceCount = pieceCount;
        _data = new byte[(pieceCount + 7) / 8];
    }

    public Bitfield(int pieceCount, byte[] data)
    {
        PieceCount = pieceCount;
        _data = data;
    }

    public int PieceCount { get; }

    public bool this[int index]
    {
        get => IsPieceAvailable(index);
        set => SetPieceAvailable(index, value);
    }

    public bool IsPieceAvailable(int index)
    {
        int byteIdx = index / 8;
        int bitIdx = 7 - (index % 8);
        return (_data[byteIdx] & (1 << bitIdx)) != 0;
    }

    public bool HasPiece(int index) => IsPieceAvailable(index);

    public void SetPieceAvailable(int index, bool available)
    {
        int byteIdx = index / 8;
        int bitIdx = 7 - (index % 8);
        if (available)
            _data[byteIdx] |= (byte)(1 << bitIdx);
        else
            _data[byteIdx] &= (byte)~(1 << bitIdx);
    }

    public void SetAll(bool value)
    {
        for (int i = 0; i < PieceCount; i++)
            SetPieceAvailable(i, value);
    }

    public int GetAvailablePiecesCount()
    {
        int count = 0;
        for (int i = 0; i < PieceCount; i++)
            if (IsPieceAvailable(i)) count++;
        return count;
    }

    public int RemainingPiecesCount() => PieceCount - GetAvailablePiecesCount();

    public void Union(Bitfield other)
    {
        for (int i = 0; i < Math.Min(_data.Length, other._data.Length); i++)
            _data[i] |= other._data[i];
    }

    public bool NotSubset(Bitfield other)
    {
        for (int i = 0; i < PieceCount; i++)
            if (IsPieceAvailable(i) && !other.IsPieceAvailable(i))
                return true;
        return false;
    }

    public byte[] ToBytes() => _data;

    public override string ToString() =>
        PieceCount == 0
            ? "0%"
            : $"{GetAvailablePiecesCount() * 100.0 / PieceCount:F0}%";
}
