using System.Security.Cryptography;
using TorrentCs.Data.Pieces;

namespace TorrentCs.Data;

public class PieceCheckerHandler : IPieceDataHandler
{
    private readonly IBlockDataHandler _inner;
    private readonly Dictionary<int, List<Block>> _pendingBlocks = new();
    private readonly HashSet<Piece> _completedPieces = new();

    public PieceCheckerHandler(IBlockDataHandler inner, Metainfo metainfo)
    {
        _inner = inner;
        Metainfo = metainfo;
    }

    public event Action<Piece>? PieceCorrupted;
    public event Action<Piece>? PieceCompleted;

    public Metainfo Metainfo { get; }
    public IReadOnlyCollection<Piece> CompletedPieces => _completedPieces;

    public byte[] ReadBlockData(long offset, int length) => _inner.ReadBlockData(offset, length);

    public bool TryReadBlockData(long offset, int length, out byte[] data) =>
        _inner.TryReadBlockData(offset, length, out data);

    public void WriteBlockData(long offset, byte[] data)
    {
        int pieceIndex = (int)(offset / Metainfo.PieceSize);
        int blockOffset = (int)(offset % Metainfo.PieceSize);
        var block = new Block(pieceIndex, blockOffset, data);

        if (!_pendingBlocks.TryGetValue(pieceIndex, out var blocks))
        {
            blocks = new List<Block>();
            _pendingBlocks[pieceIndex] = blocks;
        }
        blocks.Add(block);

        CheckPiece(pieceIndex);
    }

    public void MarkPieceAsCompleted(Piece piece)
    {
        _completedPieces.Add(piece);
        PieceCompleted?.Invoke(piece);
    }

    public void Flush() => _inner.Flush();

    private void CheckPiece(int pieceIndex)
    {
        if (!_pendingBlocks.TryGetValue(pieceIndex, out var blocks))
            return;

        var piece = Metainfo.Pieces[pieceIndex];
        var sorted = blocks.OrderBy(b => b.Offset).ToList();

        // Verify the blocks are contiguous and cover the full piece
        int expectedOffset = 0;
        foreach (var block in sorted)
        {
            if (block.Offset != expectedOffset) return;
            expectedOffset += block.Length;
        }
        if (expectedOffset < piece.Size) return;

        VerifyAndCommit(piece, sorted);
    }

    private void VerifyAndCommit(Piece piece, List<Block> sortedBlocks)
    {
        var data = new byte[piece.Size];
        int pos = 0;
        foreach (var block in sortedBlocks)
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
            PieceCompleted?.Invoke(piece);
        }
        else
        {
            PieceCorrupted?.Invoke(piece);
        }
    }
}
