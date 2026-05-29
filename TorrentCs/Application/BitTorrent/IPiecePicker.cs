using TorrentCs.Data;

namespace TorrentCs.Application.BitTorrent;

public interface IPiecePicker
{
    IEnumerable<BlockRequest> BlocksToRequest(
        IEnumerable<Piece> incompletePieces,
        Bitfield availability,
        IReadOnlyCollection<IPeer> peers,
        IReadOnlyCollection<BlockRequest> requestedBlocks);
}
