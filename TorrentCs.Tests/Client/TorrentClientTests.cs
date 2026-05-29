using TorrentCs;
using TorrentCs.Data;

namespace TorrentCs.Tests.Client;

[Collection("TorrentClient")]
public class TorrentClientTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private ITorrentClient? _client;

    public TorrentClientTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose()
    {
        _client?.Dispose();
        try { Directory.Delete(_tempDir, recursive: true); }
        catch { /* best-effort */ }
    }

    private static Metainfo BuildMetainfo() =>
        new MetainfoBuilder("test")
            .AddFile("file.bin", new byte[1024])
            .WithPieceSize(512)
            .WithTracker("udp://tracker.invalid:6969")
            .Build();

    [Fact]
    public void Create_ReturnsClient()
    {
        _client = TorrentClient.Create();
        Assert.NotNull(_client);
    }

    [Fact]
    public void Create_HasLocalPeerId()
    {
        _client = TorrentClient.Create();
        Assert.NotNull(_client.LocalPeerId);
        Assert.Equal(20, _client.LocalPeerId.Value.Length);
    }

    [Fact]
    public void Create_InitiallyNoDownloads()
    {
        _client = TorrentClient.Create();
        Assert.Empty(_client.Downloads);
    }

    [Fact]
    public void Add_FromMetainfo_RegistersDownload()
    {
        _client = TorrentClient.Create();
        var download = _client.Add(BuildMetainfo(), _tempDir);

        Assert.NotNull(download);
        Assert.Single(_client.Downloads);
    }

    [Fact]
    public void Add_FromMetainfo_DownloadHasDescription()
    {
        _client = TorrentClient.Create();
        var download = _client.Add(BuildMetainfo(), _tempDir);

        Assert.Equal("test", download.Description.Name);
    }

    [Fact]
    public void Add_DownloadInitialStateIsDownloading()
    {
        _client = TorrentClient.Create();
        var download = _client.Add(BuildMetainfo(), _tempDir);

        Assert.Equal(DownloadState.Downloading, download.State);
    }

    [Fact]
    public void Add_FromStream_ParsesAndRegisters()
    {
        _client = TorrentClient.Create();

        // Build a real .torrent stream via Bencode
        var torrentStream = BuildTorrentStream();
        var download = _client.Add(torrentStream, _tempDir);

        Assert.NotNull(download);
        Assert.Single(_client.Downloads);
    }

    [Fact]
    public void Add_InitialProgressIsZero()
    {
        _client = TorrentClient.Create();
        var download = _client.Add(BuildMetainfo(), _tempDir);
        Assert.Equal(0, download.Progress);
    }

    private static Stream BuildTorrentStream()
    {
        var info = new BencodeNET.Objects.BDictionary
        {
            ["name"] = new BencodeNET.Objects.BString("streamed.bin", System.Text.Encoding.UTF8),
            ["piece length"] = new BencodeNET.Objects.BNumber(512),
            ["pieces"] = new BencodeNET.Objects.BString(new byte[20], System.Text.Encoding.Latin1),
            ["length"] = new BencodeNET.Objects.BNumber(100),
        };
        var torrent = new BencodeNET.Objects.BDictionary
        {
            ["announce"] = new BencodeNET.Objects.BString("udp://tracker.invalid:6969", System.Text.Encoding.UTF8),
            ["info"] = info,
        };
        var ms = new MemoryStream();
        torrent.EncodeTo(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }
}
