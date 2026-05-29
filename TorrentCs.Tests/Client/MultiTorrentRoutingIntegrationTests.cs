using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using TorrentCs;
using TorrentCs.Data;
using TorrentCs.Tracker;
using TorrentCs.Transport.Tcp;

namespace TorrentCs.Tests.Client;

/// <summary>
/// Verifies that a client seeding several torrents routes each incoming connection to the right
/// torrent by info-hash. The seeder holds two torrents; a leecher downloads the second one, so a
/// naive "hand to the first torrent" router would fail the handshake and time out.
/// </summary>
[Collection("TorrentClient")]
public class MultiTorrentRoutingIntegrationTests : IDisposable
{
    private const int SeederPort = 6922;
    private const int LeecherPort = 6923;

    private readonly string _root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private ITorrentClient? _seeder;
    private ITorrentClient? _leecher;

    public MultiTorrentRoutingIntegrationTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        _leecher?.Dispose();
        _seeder?.Dispose();
        try { Directory.Delete(_root, recursive: true); }
        catch { /* best-effort */ }
    }

    [Fact(Timeout = 60000)]
    public async Task Leecher_DownloadsSecondTorrent_RoutedByInfoHash()
    {
        // Two distinct torrents (different content → different info-hash).
        var (metaA, dataA) = MakeTorrent("torrent-a.bin", seed: 1);
        var (metaB, dataB) = MakeTorrent("torrent-b.bin", seed: 2);
        Assert.NotEqual(metaA.InfoHash, metaB.InfoHash);

        // Seeder holds both, with A added first so a first-torrent router would mis-route.
        var seederDirA = SeedDir("a", "torrent-a.bin", dataA);
        var seederDirB = SeedDir("b", "torrent-b.bin", dataB);

        _seeder = TorrentClientBuilder.CreateDefaultBuilder().UsePort(SeederPort).Build();
        _seeder.Add(metaA, seederDirA).Start();
        _seeder.Add(metaB, seederDirB).Start();
        await Task.Delay(500);

        // Leecher wants torrent B only.
        var leecherDir = Path.Combine(_root, "leecher-b");
        _leecher = TorrentClientBuilder.CreateDefaultBuilder()
            .UsePort(LeecherPort)
            .ConfigureServices(s =>
                s.AddSingleton<ITrackerClientFactory>(new StubTrackerClientFactory(SeederPort)))
            .Build();
        var download = _leecher.Add(metaB, leecherDir);
        download.Start();

        await download.WaitForDownloadCompletionAsync(TimeSpan.FromSeconds(40));
        Assert.Equal(DownloadState.Completed, download.State);

        _leecher.Dispose();
        _leecher = null;

        var downloaded = await File.ReadAllBytesAsync(Path.Combine(leecherDir, "torrent-b.bin"));
        Assert.Equal(SHA1.HashData(dataB), SHA1.HashData(downloaded));
    }

    private static (Metainfo Meta, byte[] Data) MakeTorrent(string name, int seed)
    {
        var data = new byte[128 * 1024];
        new Random(seed).NextBytes(data);
        var meta = new MetainfoBuilder(name)
            .AddFile(name, data)
            .WithPieceSize(32 * 1024)
            .WithTracker("http://stub.invalid/announce")
            .Build();
        return (meta, data);
    }

    private string SeedDir(string subdir, string fileName, byte[] data)
    {
        var dir = Path.Combine(_root, "seeder-" + subdir);
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(Path.Combine(dir, fileName), data);
        return dir;
    }

    private sealed class StubTrackerClientFactory(int seederPort) : ITrackerClientFactory
    {
        public ITracker? CreateTrackerClient(Uri trackerUri) => new StubTracker(seederPort);
    }

    private sealed class StubTracker(int seederPort) : ITracker
    {
        public string Type => "Stub";

        public Task<AnnounceResult> Announce(AnnounceRequest request)
        {
            var seeder = new TcpTransportStream(IPAddress.Any, IPAddress.Loopback, seederPort);
            return Task.FromResult(new AnnounceResult([seeder]));
        }
    }
}
