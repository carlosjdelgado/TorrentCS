using TorrentCs.Data;
using TorrentCs.Data.Pieces;

namespace TorrentCs.Tests.Data.Pieces;

public class PieceDataHandlerExtensionsTests
{
    [Fact]
    public void IncompletePieces_WithNoCompleted_ReturnsAllPieces()
    {
        var meta = BuildMeta();
        using var fh = new MemoryFileHandler();
        var handler = new PieceCheckerHandler(new BlockDataHandler(fh, meta), meta);

        var incomplete = handler.IncompletePieces().ToList();
        Assert.Equal(meta.Pieces.Count, incomplete.Count);
    }

    [Fact]
    public void IncompletePieces_AfterCompletingOne_ExcludesIt()
    {
        var meta = BuildMeta(128);
        using var fh = new MemoryFileHandler();
        var handler = new PieceCheckerHandler(new BlockDataHandler(fh, meta), meta);

        handler.MarkPieceAsCompleted(meta.Pieces[0]);
        var incomplete = handler.IncompletePieces().ToList();

        Assert.Equal(meta.Pieces.Count - 1, incomplete.Count);
        Assert.DoesNotContain(meta.Pieces[0], incomplete);
    }

    private static Metainfo BuildMeta(int pieceSize = 256) =>
        new MetainfoBuilder("test")
            .AddFile("f.bin", new byte[pieceSize * 3])
            .WithPieceSize(pieceSize)
            .Build();
}
