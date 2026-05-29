using TorrentCs.Application.BitTorrent;
using TorrentCs.Data;
using TorrentCs.Modularity;

namespace TorrentCs.Tests.Application.BitTorrent;

public class PiecePickerTests
{
    private static Metainfo BuildMeta(int pieceCount, int pieceSize = 16384) =>
        new MetainfoBuilder("test")
            .AddFile("f.bin", new byte[pieceCount * pieceSize])
            .WithPieceSize(pieceSize)
            .Build();

    [Fact]
    public void BlocksToRequest_NoIncompletePieces_ReturnsEmpty()
    {
        var meta = BuildMeta(4);
        var picker = new PiecePicker();
        var availability = new Bitfield(4); availability.SetAll(true);

        var blocks = picker.BlocksToRequest([], availability, [], []);
        Assert.Empty(blocks);
    }

    [Fact]
    public void BlocksToRequest_UnavailablePiece_Skips()
    {
        var meta = BuildMeta(4);
        var picker = new PiecePicker();
        var availability = new Bitfield(4); // all false

        var blocks = picker.BlocksToRequest(meta.Pieces, availability, [], []);
        Assert.Empty(blocks);
    }

    [Fact]
    public void BlocksToRequest_AvailablePiece_ReturnsSomeBlocks()
    {
        var meta = BuildMeta(4);
        var picker = new PiecePicker();
        var availability = new Bitfield(4); availability[0] = true;

        var blocks = picker.BlocksToRequest(meta.Pieces, availability, [], []).ToList();
        Assert.NotEmpty(blocks);
        Assert.All(blocks, b => Assert.Equal(0, b.PieceIndex));
    }

    [Fact]
    public void BlocksToRequest_AlreadyRequested_Skipped()
    {
        // Single-block piece: once requested nothing more to ask
        var meta = BuildMeta(1, pieceSize: 16384);
        var picker = new PiecePicker();
        var availability = new Bitfield(1); availability[0] = true;

        var firstPass = picker.BlocksToRequest(meta.Pieces, availability, [], []).ToList();
        Assert.Single(firstPass);

        var secondPass = picker.BlocksToRequest(meta.Pieces, availability, [], firstPass).ToList();
        Assert.Empty(secondPass);
    }

    [Fact]
    public void BlocksToRequest_RespectsPieceSize()
    {
        var meta = BuildMeta(1, pieceSize: 16384);
        var picker = new PiecePicker();
        var availability = new Bitfield(1); availability[0] = true;

        var blocks = picker.BlocksToRequest(meta.Pieces, availability, [], []).ToList();
        Assert.Single(blocks); // exactly 1 block = 1 piece
        Assert.Equal(16384, blocks[0].Length);
    }
}
