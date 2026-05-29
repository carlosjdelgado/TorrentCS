using System.Security.Cryptography;
using TorrentCs.Data.Pieces;

namespace TorrentCs.Data;

public class PieceCheckerHandler : IPieceDataHandler
{
    private readonly IBlockDataHandler _inner;
    private readonly Dictionary<int, List<Block>> _pendingBlocks = [];
    private readonly HashSet<Piece> _completedPieces = [];
    private readonly object _lock = new();

    public PieceCheckerHandler(IBlockDataHandler inner, Metainfo metainfo)
    {
        _inner = inner;
        Metainfo = metainfo;
    }

    public event Action<Piece>? PieceCorrupted;
    public event Action<Piece>? PieceCompleted;

    public Metainfo Metainfo { get; }

    // Snapshot: read from the download/verify threads while the message threads mutate it.
    public IReadOnlyCollection<Piece> CompletedPieces
    {
        get { lock (_lock) return _completedPieces.ToList(); }
    }

    public byte[] ReadBlockData(long offset, int length) => _inner.ReadBlockData(offset, length);

    public bool TryReadBlockData(long offset, int length, out byte[] data) =>
        _inner.TryReadBlockData(offset, length, out data);

    public void WriteBlockData(long offset, byte[] data)
    {
        int pieceIndex = (int)(offset / Metainfo.PieceSize);
        int blockOffset = (int)(offset % Metainfo.PieceSize);
        var block = new Block(pieceIndex, blockOffset, data);

        Piece? completed = null;
        Piece? corrupted = null;
        lock (_lock)
        {
            if (!_pendingBlocks.TryGetValue(pieceIndex, out var blocks))
            {
                blocks = [];
                _pendingBlocks[pieceIndex] = blocks;
            }
            blocks.Add(block);
            TryCompletePiece(pieceIndex, ref completed, ref corrupted);
        }

        // Raise events outside the lock to avoid holding it during subscriber callbacks.
        if (completed is not null) PieceCompleted?.Invoke(completed);
        if (corrupted is not null) PieceCorrupted?.Invoke(corrupted);
    }

    public void MarkPieceAsCompleted(Piece piece)
    {
        lock (_lock) _completedPieces.Add(piece);
        PieceCompleted?.Invoke(piece);
    }

    public void Flush() => _inner.Flush();

    // Called under _lock. If pieceIndex's blocks are contiguous and complete, verifies and commits
    // the piece, reporting the outcome via the ref parameters.
    private void TryCompletePiece(int pieceIndex, ref Piece? completed, ref Piece? corrupted)
    {
        if (!_pendingBlocks.TryGetValue(pieceIndex, out var blocks))
            return;

        var piece = Metainfo.Pieces[pieceIndex];
        var sorted = blocks.OrderBy(b => b.Offset).ToList();

        int expectedOffset = 0;
        foreach (var block in sorted)
        {
            if (block.Offset != expectedOffset) return;
            expectedOffset += block.Length;
        }
        if (expectedOffset < piece.Size) return;

        var data = new byte[piece.Size];
        int pos = 0;
        foreach (var block in sorted)
        {
            Array.Copy(block.Data, 0, data, pos, block.Length);
            pos += block.Length;
        }

        _pendingBlocks.Remove(piece.Index);

        var hash = new Sha1Hash(SHA1.HashData(data));
        if (hash == piece.Hash)
        {
            _inner.WriteBlockData(Metainfo.PieceOffset(piece), data);
            _completedPieces.Add(piece);
            completed = piece;
        }
        else
        {
            corrupted = piece;
        }
    }
}
