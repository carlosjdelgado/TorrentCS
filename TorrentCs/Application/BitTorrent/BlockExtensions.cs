using TorrentCs.Data;

namespace TorrentCs.Application.BitTorrent;

public static class BlockExtensions
{
    public static BlockRequest AsRequest(this Block block) =>
        new(block.PieceIndex, block.Offset, block.Length);

    public static Block ToBlock(this BlockRequest request, byte[] data) =>
        new(request.PieceIndex, request.Offset, data);
}
