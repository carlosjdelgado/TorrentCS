using TorrentCs.Data;

namespace TorrentCs.Tests.Data;

public class FileResumeStoreTests : IDisposable
{
    private readonly string _dir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private readonly FileResumeStore _store = new();

    public FileResumeStoreTests() => Directory.CreateDirectory(_dir);

    public void Dispose()
    {
        try { Directory.Delete(_dir, recursive: true); }
        catch { /* best-effort */ }
    }

    private static Sha1Hash Hash(byte seed)
    {
        var bytes = new byte[20];
        bytes[0] = seed;
        return new Sha1Hash(bytes);
    }

    [Fact]
    public void Load_NoFile_ReturnsNull()
    {
        Assert.Null(_store.Load(_dir, Hash(1), pieceCount: 10));
    }

    [Fact]
    public void SaveThenLoad_RoundtripsCompletedPieces()
    {
        var infoHash = Hash(1);
        _store.Save(_dir, new ResumeData(infoHash, pieceCount: 20, completedPieces: [0, 5, 19]));

        var loaded = _store.Load(_dir, infoHash, pieceCount: 20);

        Assert.NotNull(loaded);
        Assert.Equal([0, 5, 19], loaded!.CompletedPieces);
        Assert.Equal(20, loaded.PieceCount);
    }

    [Fact]
    public void Load_DifferentInfoHash_ReturnsNull()
    {
        _store.Save(_dir, new ResumeData(Hash(1), pieceCount: 10, completedPieces: [1, 2]));
        Assert.Null(_store.Load(_dir, Hash(2), pieceCount: 10));
    }

    [Fact]
    public void Load_DifferentPieceCount_ReturnsNull()
    {
        var infoHash = Hash(1);
        _store.Save(_dir, new ResumeData(infoHash, pieceCount: 10, completedPieces: [1, 2]));
        Assert.Null(_store.Load(_dir, infoHash, pieceCount: 11));
    }

    [Fact]
    public void Save_Overwrites_PreviousState()
    {
        var infoHash = Hash(1);
        _store.Save(_dir, new ResumeData(infoHash, pieceCount: 10, completedPieces: [1]));
        _store.Save(_dir, new ResumeData(infoHash, pieceCount: 10, completedPieces: [1, 2, 3]));

        var loaded = _store.Load(_dir, infoHash, pieceCount: 10);
        Assert.Equal([1, 2, 3], loaded!.CompletedPieces);
    }

    [Fact]
    public void Load_CorruptFile_ReturnsNull()
    {
        var infoHash = Hash(1);
        _store.Save(_dir, new ResumeData(infoHash, pieceCount: 10, completedPieces: [1]));

        // Truncate the resume file to corrupt it.
        var resumeFile = Directory.GetFiles(_dir, "*.resume").Single();
        File.WriteAllBytes(resumeFile, new byte[] { 0x01, 0x02 });

        Assert.Null(_store.Load(_dir, infoHash, pieceCount: 10));
    }

    [Fact]
    public void SaveThenLoad_AllPiecesComplete()
    {
        var infoHash = Hash(3);
        var all = Enumerable.Range(0, 16).ToArray();
        _store.Save(_dir, new ResumeData(infoHash, pieceCount: 16, completedPieces: all));

        var loaded = _store.Load(_dir, infoHash, pieceCount: 16);
        Assert.Equal(all, loaded!.CompletedPieces);
    }

    [Fact]
    public void SaveThenLoad_NoPiecesComplete()
    {
        var infoHash = Hash(4);
        _store.Save(_dir, new ResumeData(infoHash, pieceCount: 8, completedPieces: []));

        var loaded = _store.Load(_dir, infoHash, pieceCount: 8);
        Assert.NotNull(loaded);
        Assert.Empty(loaded!.CompletedPieces);
    }
}
