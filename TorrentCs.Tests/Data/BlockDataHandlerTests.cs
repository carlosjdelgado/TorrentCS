using TorrentCs.Data;

namespace TorrentCs.Tests.Data;

public class BlockDataHandlerTests
{
    [Fact]
    public void WriteAndRead_Roundtrip_SingleFile()
    {
        var meta = SingleFileMetainfo(512);
        using var fileHandler = new MemoryFileHandler();
        var handler = new BlockDataHandler(fileHandler, meta);

        var data = new byte[] { 1, 2, 3, 4, 5 };
        handler.WriteBlockData(10, data);
        var result = handler.ReadBlockData(10, 5);

        Assert.Equal(data, result);
    }

    [Fact]
    public void WriteAndRead_Roundtrip_AcrossTwoFiles()
    {
        // a.bin = 100 bytes, b.bin = 100 bytes; write 20 bytes spanning the boundary
        var meta = TwoFileMetainfo(100, 100, 256);
        using var fileHandler = new MemoryFileHandler();
        var handler = new BlockDataHandler(fileHandler, meta);

        var data = Enumerable.Range(0, 20).Select(i => (byte)i).ToArray();
        handler.WriteBlockData(90, data); // bytes 90..109: 10 in a.bin, 10 in b.bin

        var result = handler.ReadBlockData(90, 20);
        Assert.Equal(data, result);
    }

    [Fact]
    public void TryReadBlockData_ReturnsFalseForUnwrittenData()
    {
        var meta = SingleFileMetainfo(256);
        using var fileHandler = new MemoryFileHandler();
        var handler = new BlockDataHandler(fileHandler, meta);

        var ok = handler.TryReadBlockData(0, 128, out _);
        Assert.False(ok);
    }

    [Fact]
    public void TryReadBlockData_ReturnsTrueAfterWrite()
    {
        var meta = SingleFileMetainfo(256);
        using var fileHandler = new MemoryFileHandler();
        var handler = new BlockDataHandler(fileHandler, meta);

        handler.WriteBlockData(0, new byte[128]);
        var ok = handler.TryReadBlockData(0, 128, out var data);

        Assert.True(ok);
        Assert.Equal(128, data.Length);
    }

    [Fact]
    public void Flush_DoesNotThrow()
    {
        var meta = SingleFileMetainfo(256);
        using var fileHandler = new MemoryFileHandler();
        var handler = new BlockDataHandler(fileHandler, meta);
        handler.Flush();
    }

    [Fact]
    public void Metainfo_IsReturnedAsProvided()
    {
        var meta = SingleFileMetainfo(256);
        using var fileHandler = new MemoryFileHandler();
        var handler = new BlockDataHandler(fileHandler, meta);
        Assert.Same(meta, handler.Metainfo);
    }

    private static Metainfo SingleFileMetainfo(int size, int pieceSize = 256) =>
        new MetainfoBuilder("test").AddFile("file.bin", new byte[size]).WithPieceSize(pieceSize).Build();

    private static Metainfo TwoFileMetainfo(int sizeA, int sizeB, int pieceSize = 256) =>
        new MetainfoBuilder("test")
            .AddFile("a.bin", new byte[sizeA])
            .AddFile("b.bin", new byte[sizeB])
            .WithPieceSize(pieceSize)
            .Build();
}
