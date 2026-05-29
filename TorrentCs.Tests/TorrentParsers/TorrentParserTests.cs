using System.Security.Cryptography;
using System.Text;
using BencodeNET.Objects;
using TorrentCs.Data;
using TorrentCs.TorrentParsers;

namespace TorrentCs.Tests.TorrentParsers;

public class TorrentParserTests
{
    // ─── Single-file tests ──────────────────────────────────────────────────

    [Fact]
    public void SingleFile_ParsesNameCorrectly()
    {
        var meta = TorrentParser.ReadFromStream(SingleFileTorrent(name: "ubuntu.iso"));
        Assert.Equal("ubuntu.iso", meta.Name);
    }

    [Fact]
    public void SingleFile_ParsesSingleContainedFile()
    {
        var meta = TorrentParser.ReadFromStream(SingleFileTorrent(name: "test.txt", length: 1024));
        Assert.Single(meta.Files);
        Assert.Equal("test.txt", meta.Files[0].Name);
        Assert.Equal(1024L, meta.Files[0].Size);
    }

    [Fact]
    public void SingleFile_ParsesPieceSize()
    {
        var meta = TorrentParser.ReadFromStream(SingleFileTorrent(pieceSize: 262144));
        Assert.Equal(262144, meta.PieceSize);
    }

    [Fact]
    public void SingleFile_CorrectPieceCount()
    {
        // 1000 bytes, 512 byte pieces → 2 pieces
        var meta = TorrentParser.ReadFromStream(SingleFileTorrent(length: 1000, pieceSize: 512));
        Assert.Equal(2, meta.Pieces.Count);
    }

    [Fact]
    public void SingleFile_LastPieceSizeIsRemainder()
    {
        // 700 bytes, 512 byte pieces → piece[0]=512, piece[1]=188
        var meta = TorrentParser.ReadFromStream(SingleFileTorrent(length: 700, pieceSize: 512));
        Assert.Equal(512, meta.Pieces[0].Size);
        Assert.Equal(188, meta.Pieces[1].Size);
    }

    [Fact]
    public void SingleFile_PieceHashIsPreserved()
    {
        var hash = new byte[20];
        hash[0] = 0xAB; hash[19] = 0xCD;

        var meta = TorrentParser.ReadFromStream(SingleFileTorrent(length: 100, pieceSize: 512, pieceHash: hash));
        Assert.Equal(hash, meta.Pieces[0].Hash!.Value);
    }

    [Fact]
    public void SingleFile_InfoHashIsComputedFromBencode()
    {
        var meta = TorrentParser.ReadFromStream(SingleFileTorrent());
        Assert.NotNull(meta.InfoHash);
        Assert.Equal(Sha1Hash.Length, meta.InfoHash.Value.Length);
        Assert.NotEqual(Sha1Hash.Empty, meta.InfoHash);
    }

    [Fact]
    public void SingleFile_RawInfoDictIsStored()
    {
        var meta = TorrentParser.ReadFromStream(SingleFileTorrent());
        Assert.NotNull(meta.RawInfoDict);
        Assert.True(meta.RawInfoDict.Length > 0);
    }

    [Fact]
    public void SingleFile_InfoHashMatchesSha1OfRawInfoDict()
    {
        var meta = TorrentParser.ReadFromStream(SingleFileTorrent());
        var expected = new Sha1Hash(SHA1.HashData(meta.RawInfoDict));
        Assert.Equal(expected, meta.InfoHash);
    }

    // ─── Multi-file tests ───────────────────────────────────────────────────

    [Fact]
    public void MultiFile_ParsesCorrectFileCount()
    {
        var files = new[] { ("a.txt", 512L), ("b.txt", 1024L), ("c.txt", 256L) };
        var meta = TorrentParser.ReadFromStream(MultiFileTorrent(files: files));
        Assert.Equal(3, meta.Files.Count);
    }

    [Fact]
    public void MultiFile_FileSizesAreCorrect()
    {
        var files = new[] { ("a.txt", 512L), ("b.txt", 1024L) };
        var meta = TorrentParser.ReadFromStream(MultiFileTorrent(files: files));
        Assert.Equal(512L, meta.Files[0].Size);
        Assert.Equal(1024L, meta.Files[1].Size);
    }

    [Fact]
    public void MultiFile_PathIncludesTorrentName()
    {
        var meta = TorrentParser.ReadFromStream(
            MultiFileTorrent(name: "mydir", files: [("a.txt", 100L)]));

        Assert.Contains("mydir", meta.Files[0].Name);
        Assert.Contains("a.txt", meta.Files[0].Name);
    }

    [Fact]
    public void MultiFile_SubdirectoryPathIsPreserved()
    {
        var meta = TorrentParser.ReadFromStream(
            MultiFileTorrent(files: [("sub/nested.txt", 100L)]));

        Assert.Contains("sub", meta.Files[0].Name);
        Assert.Contains("nested.txt", meta.Files[0].Name);
    }

    [Fact]
    public void MultiFile_PieceCountIsBasedOnTotalSize()
    {
        // 2 files * 256 bytes = 512 bytes, pieceSize=512 → 1 piece
        var meta = TorrentParser.ReadFromStream(
            MultiFileTorrent(pieceSize: 512, files: [("a.bin", 256L), ("b.bin", 256L)]));

        Assert.Single(meta.Pieces);
        Assert.Equal(512, meta.Pieces[0].Size);
    }

    // ─── Tracker tests ──────────────────────────────────────────────────────

    [Fact]
    public void Tracker_AnnounceUrlIsParsed()
    {
        var meta = TorrentParser.ReadFromStream(
            SingleFileTorrent(announce: "udp://tracker.example.com:6969"));

        Assert.Contains("udp://tracker.example.com:6969", meta.Trackers);
    }

    [Fact]
    public void Tracker_AnnounceListMergedWithAnnounce()
    {
        var meta = TorrentParser.ReadFromStream(SingleFileTorrent(
            announce: "udp://t1.example.com",
            announceList: ["udp://t2.example.com", "udp://t3.example.com"]));

        Assert.Equal(3, meta.Trackers.Count);
    }

    [Fact]
    public void Tracker_DuplicatesAreDeduped()
    {
        var meta = TorrentParser.ReadFromStream(SingleFileTorrent(
            announce: "udp://same.example.com",
            announceList: ["udp://same.example.com", "udp://other.example.com"]));

        var count = meta.Trackers.Count(t => t.Equals("udp://same.example.com", StringComparison.OrdinalIgnoreCase));
        Assert.Equal(1, count);
    }

    [Fact]
    public void Tracker_NoAnnounce_EmptyTrackers()
    {
        var meta = TorrentParser.ReadFromStream(SingleFileTorrent(announce: null));
        Assert.Empty(meta.Trackers);
    }

    // ─── Helpers ────────────────────────────────────────────────────────────

    private static Stream SingleFileTorrent(
        string name = "test.txt",
        long length = 100,
        int pieceSize = 512,
        byte[]? pieceHash = null,
        string? announce = "udp://tracker.example.com:6969",
        IEnumerable<string>? announceList = null)
    {
        int numPieces = (int)Math.Ceiling((double)length / pieceSize);
        pieceHash ??= new byte[20];
        var allHashes = Enumerable.Range(0, numPieces)
            .SelectMany(_ => pieceHash)
            .ToArray();

        var info = new BDictionary
        {
            ["name"] = new BString(name, Encoding.UTF8),
            ["piece length"] = new BNumber(pieceSize),
            ["pieces"] = new BString(allHashes, Encoding.Latin1),
            ["length"] = new BNumber(length),
        };

        return BuildTorrent(info, announce, announceList);
    }

    private static Stream MultiFileTorrent(
        string name = "mydir",
        int pieceSize = 512,
        IEnumerable<(string path, long size)>? files = null,
        string? announce = "udp://tracker.example.com:6969")
    {
        files ??= [("a.txt", 256), ("b.txt", 256)];
        long total = files.Sum(f => f.size);
        int numPieces = (int)Math.Ceiling((double)total / pieceSize);

        var filesList = new BList();
        foreach (var (path, size) in files)
        {
            var parts = path.Split('/');
            var pathList = new BList();
            foreach (var p in parts) pathList.Add(new BString(p, Encoding.UTF8));
            filesList.Add(new BDictionary
            {
                ["length"] = new BNumber(size),
                ["path"] = pathList,
            });
        }

        var info = new BDictionary
        {
            ["name"] = new BString(name, Encoding.UTF8),
            ["piece length"] = new BNumber(pieceSize),
            ["pieces"] = new BString(new byte[numPieces * 20], Encoding.Latin1),
            ["files"] = filesList,
        };

        return BuildTorrent(info, announce);
    }

    private static Stream BuildTorrent(BDictionary info, string? announce, IEnumerable<string>? announceList = null)
    {
        var torrent = new BDictionary { ["info"] = info };

        if (announce is not null)
            torrent["announce"] = new BString(announce, Encoding.UTF8);

        if (announceList is not null)
        {
            var tierList = new BList();
            foreach (var url in announceList)
                tierList.Add(new BList { new BString(url, Encoding.UTF8) });
            torrent["announce-list"] = tierList;
        }

        var ms = new MemoryStream();
        torrent.EncodeTo(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
}
