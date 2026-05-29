using TorrentCs.Data;

namespace TorrentCs.Tests.Data;

public class PieceCheckerHandlerTests
{
    [Fact]
    public void WriteBlockData_CompletePiece_FiresPieceCompleted()
    {
        var fileData = new byte[256];
        new Random(0).NextBytes(fileData);
        var (meta, checker, fh) = Build(fileData, 256);
        using (fh)
        {
            Piece? completed = null;
            checker.PieceCompleted += p => completed = p;

            checker.WriteBlockData(0, fileData);

            Assert.NotNull(completed);
            Assert.Equal(0, completed!.Index);
        }
    }

    [Fact]
    public void WriteBlockData_CorruptData_FiresPieceCorrupted()
    {
        var fileData = new byte[256];
        var (meta, checker, fh) = Build(fileData, 256);
        using (fh)
        {
            Piece? corrupted = null;
            checker.PieceCorrupted += p => corrupted = p;

            checker.WriteBlockData(0, new byte[256].Select(_ => (byte)0xFF).ToArray());

            Assert.NotNull(corrupted);
        }
    }

    [Fact]
    public void WriteBlockData_InTwoBlocks_CompletesAfterSecond()
    {
        var fileData = new byte[256];
        new Random(1).NextBytes(fileData);
        var (meta, checker, fh) = Build(fileData, 256);
        using (fh)
        {
            Piece? completed = null;
            checker.PieceCompleted += p => completed = p;

            checker.WriteBlockData(0, fileData[..128]);
            Assert.Null(completed);

            checker.WriteBlockData(128, fileData[128..]);
            Assert.NotNull(completed);
        }
    }

    [Fact]
    public void WriteBlockData_ValidPiece_AddsToCompletedPieces()
    {
        var fileData = new byte[256];
        var (meta, checker, fh) = Build(fileData, 256);
        using (fh)
        {
            checker.WriteBlockData(0, fileData);
            Assert.Contains(meta.Pieces[0], checker.CompletedPieces);
        }
    }

    [Fact]
    public void MarkPieceAsCompleted_AddsToPiecesAndFiresEvent()
    {
        var fileData = new byte[256];
        var (meta, checker, fh) = Build(fileData, 256);
        using (fh)
        {
            Piece? completed = null;
            checker.PieceCompleted += p => completed = p;

            checker.MarkPieceAsCompleted(meta.Pieces[0]);

            Assert.Same(meta.Pieces[0], completed);
            Assert.Contains(meta.Pieces[0], checker.CompletedPieces);
        }
    }

    [Fact]
    public void ReadBlockData_DelegatesToInner()
    {
        var fileData = new byte[256];
        new Random(2).NextBytes(fileData);
        var (meta, checker, fh) = Build(fileData, 256);
        using (fh)
        {
            checker.WriteBlockData(0, fileData);

            var result = checker.ReadBlockData(0, 16);
            Assert.Equal(fileData[..16], result);
        }
    }

    [Fact]
    public void MultiPiece_EachPieceCompletesIndependently()
    {
        var fileData = new byte[512];
        new Random(3).NextBytes(fileData);
        var (meta, checker, fh) = Build(fileData, 256);
        using (fh)
        {
            var completed = new List<int>();
            checker.PieceCompleted += p => completed.Add(p.Index);

            checker.WriteBlockData(0, fileData[..256]);
            checker.WriteBlockData(256, fileData[256..]);

            Assert.Equal(2, completed.Count);
            Assert.Contains(0, completed);
            Assert.Contains(1, completed);
        }
    }

    private static (Metainfo Meta, PieceCheckerHandler Checker, MemoryFileHandler FileHandler) Build(
        byte[] fileData, int pieceSize)
    {
        var meta = new MetainfoBuilder("test")
            .AddFile("f.bin", fileData)
            .WithPieceSize(pieceSize)
            .Build();

        var fh = new MemoryFileHandler();
        var inner = new BlockDataHandler(fh, meta);
        var checker = new PieceCheckerHandler(inner, meta);
        return (meta, checker, fh);
    }
}
