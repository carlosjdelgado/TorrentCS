using TorrentCs.Data;

namespace TorrentCs.Tests.Data;

public class ResumePersisterTests
{
    private static (PieceCheckerHandler Handler, Metainfo Meta, byte[] Data) BuildHandler(int pieceCount)
    {
        var data = new byte[pieceCount * 64];
        new Random(7).NextBytes(data);
        var meta = new MetainfoBuilder("test")
            .AddFile("f.bin", data)
            .WithPieceSize(64)
            .Build();
        var handler = new PieceCheckerHandler(new BlockDataHandler(new MemoryFileHandler(), meta), meta);
        return (handler, meta, data);
    }

    [Fact]
    public void Save_OnPieceCompleted_PersistsCompletedPiece()
    {
        var (handler, meta, data) = BuildHandler(pieceCount: 4);
        var store = new SpyResumeStore();

        using (new ResumePersister(store, handler, "dir", minSaveInterval: TimeSpan.Zero))
        {
            // Write piece 0's full data → completes and verifies it → fires PieceCompleted.
            handler.WriteBlockData(0, data[..64]);
        }

        Assert.NotNull(store.LastSaved);
        Assert.Contains(0, store.LastSaved!.CompletedPieces);
        Assert.Equal(meta.InfoHash, store.LastSaved.InfoHash);
    }

    [Fact]
    public void Dispose_FlushesThrottledState()
    {
        var (handler, meta, data) = BuildHandler(pieceCount: 2);
        var store = new SpyResumeStore();

        // Huge interval: the first completion saves immediately, later ones are throttled.
        var persister = new ResumePersister(store, handler, "dir", minSaveInterval: TimeSpan.FromHours(1));

        handler.WriteBlockData(0, data[..64]); // piece 0 → first save (no prior save to throttle against)
        Assert.Equal(1, store.SaveCount);

        handler.WriteBlockData(64, data[64..]); // piece 1 → throttled, pending
        Assert.Equal(1, store.SaveCount);

        persister.Dispose(); // flushes the pending state

        Assert.Equal(2, store.SaveCount);
        Assert.Contains(1, store.LastSaved!.CompletedPieces);
    }

    [Fact]
    public void Dispose_Unsubscribes_NoSaveAfterDispose()
    {
        var (handler, meta, data) = BuildHandler(pieceCount: 2);
        var store = new SpyResumeStore();

        var persister = new ResumePersister(store, handler, "dir", minSaveInterval: TimeSpan.Zero);
        persister.Dispose();
        int countAfterDispose = store.SaveCount;

        handler.WriteBlockData(0, data[..64]); // should not trigger a save anymore

        Assert.Equal(countAfterDispose, store.SaveCount);
    }

    private sealed class SpyResumeStore : IResumeStore
    {
        public int SaveCount { get; private set; }
        public ResumeData? LastSaved { get; private set; }

        public ResumeData? Load(string directory, Sha1Hash infoHash, int pieceCount) => null;

        public void Save(string directory, ResumeData data)
        {
            SaveCount++;
            LastSaved = data;
        }
    }
}
