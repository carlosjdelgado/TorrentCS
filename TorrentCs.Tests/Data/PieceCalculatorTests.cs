using System.Security.Cryptography;
using TorrentCs.Data;

namespace TorrentCs.Tests.Data;

public class PieceCalculatorTests
{
    [Fact]
    public void SingleFile_ExactPieceSize_OnePiece()
    {
        var data = new byte[256];
        var files = new List<ContainedFile> { new("f.bin", 256) };
        using var fileHandler = new MemoryFileHandler("f.bin", data);
        var pieces = new List<Piece>();

        new PieceCalculator().ComputePieces(files, 256, fileHandler, pieces);

        Assert.Single(pieces);
        Assert.Equal(0, pieces[0].Index);
        Assert.Equal(256, pieces[0].Size);
    }

    [Fact]
    public void SingleFile_TwoPieces_LastIsShorter()
    {
        var data = new byte[300];
        var files = new List<ContainedFile> { new("f.bin", 300) };
        using var fileHandler = new MemoryFileHandler("f.bin", data);
        var pieces = new List<Piece>();

        new PieceCalculator().ComputePieces(files, 256, fileHandler, pieces);

        Assert.Equal(2, pieces.Count);
        Assert.Equal(256, pieces[0].Size);
        Assert.Equal(44, pieces[1].Size);
    }

    [Fact]
    public void PieceHash_MatchesManualSha1()
    {
        var data = new byte[128];
        new Random(42).NextBytes(data);
        var expectedHash = SHA1.HashData(data);

        var files = new List<ContainedFile> { new("f.bin", 128) };
        using var fileHandler = new MemoryFileHandler("f.bin", data);
        var pieces = new List<Piece>();

        new PieceCalculator().ComputePieces(files, 256, fileHandler, pieces);

        Assert.Equal(expectedHash, pieces[0].Hash!.Value);
    }

    [Fact]
    public void MultipleFiles_PieceSpanningBothFiles()
    {
        var a = new byte[100];
        var b = new byte[100];
        var files = new List<ContainedFile> { new("a.bin", 100), new("b.bin", 100) };
        using var fileHandler = new MemoryFileHandler(
            new Dictionary<string, byte[]> { ["a.bin"] = a, ["b.bin"] = b });
        var pieces = new List<Piece>();

        new PieceCalculator().ComputePieces(files, 256, fileHandler, pieces);

        // 200 bytes / 256 = 1 piece
        Assert.Single(pieces);
        Assert.Equal(200, pieces[0].Size);
    }

    [Fact]
    public void PieceIndexes_AreSequential()
    {
        var data = new byte[1024];
        var files = new List<ContainedFile> { new("f.bin", 1024) };
        using var fileHandler = new MemoryFileHandler("f.bin", data);
        var pieces = new List<Piece>();

        new PieceCalculator().ComputePieces(files, 256, fileHandler, pieces);

        for (int i = 0; i < pieces.Count; i++)
            Assert.Equal(i, pieces[i].Index);
    }
}
