using TorrentCs.Data;

namespace TorrentCs.Application.BitTorrent;

public class PiecePicker : IPiecePicker
{
    private const int MaxOutstandingRequests = 100;
    private const int BlockRequestSize = 16384; // 16 KB

    public IEnumerable<BlockRequest> BlocksToRequest(
        IEnumerable<Piece> incompletePieces,
        Bitfield availability,
        IReadOnlyCollection<IPeer> peers,
        IReadOnlyCollection<BlockRequest> requestedBlocks)
    {
        var result = new List<BlockRequest>();

        foreach (var piece in incompletePieces)
        {
            if (result.Count >= MaxOutstandingRequests) break;
            if (!availability.IsPieceAvailable(piece.Index)) continue;

            var block = NextBlockForPiece(piece, requestedBlocks, result);
            if (block is not null)
                result.Add(block);
        }

        return result;
    }

    private static BlockRequest? NextBlockForPiece(
        Piece piece,
        IReadOnlyCollection<BlockRequest> requested,
        IList<BlockRequest> pending)
    {
        int offset = 0;
        while (offset < piece.Size)
        {
            int length = Math.Min(BlockRequestSize, piece.Size - offset);
            var block = new BlockRequest(piece.Index, offset, length);

            bool alreadyRequested = requested.Any(b => b == block) || pending.Any(b => b == block);
            if (!alreadyRequested)
                return block;

            offset += length;
        }
        return null;
    }
}
