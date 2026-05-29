using System.Net;
using System.Security.Cryptography;
using Microsoft.Extensions.DependencyInjection;
using TorrentCs;
using TorrentCs.Data;
using TorrentCs.Tracker;
using TorrentCs.Transport.Tcp;

namespace TorrentCs.Tests.Client;

/// <summary>
/// End-to-end test: one client seeds a file, a second client downloads it from the seeder over
/// the real BitTorrent wire protocol (handshake → interested → unchoke → request → piece →
/// SHA-1 verification). Peer discovery uses a stub tracker that points the leecher at the seeder,
/// so the test is fully deterministic and needs no internet access.
/// </summary>
[Collection("TorrentClient")]
public class SeederLeecherIntegrationTests : IDisposable
{
    private const int SeederPort = 6920;
    private const int LeecherPort = 6921;

    private readonly string _root = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
    private ITorrentClient? _seeder;
    private ITorrentClient? _leecher;

    public SeederLeecherIntegrationTests() => Directory.CreateDirectory(_root);

    public void Dispose()
    {
        _leecher?.Dispose();
        _seeder?.Dispose();
        try { Directory.Delete(_root, recursive: true); }
        catch { /* best-effort */ }
    }

    [Fact(Timeout = 60000)]
    public async Task Leecher_DownloadsFileFromSeeder_AndContentMatches()
    {
        const string fileName = "integration-test.bin";

        // 1. A small deterministic file spanning several pieces (256 KB over 64 KB pieces = 4).
        var original = new byte[256 * 1024];
        new Random(20260529).NextBytes(original);

        var metainfo = new MetainfoBuilder(fileName)
            .AddFile(fileName, original)
            .WithPieceSize(64 * 1024)
            .WithTracker("http://stub.invalid/announce")
            .Build();

        // 2. Seeder already has the complete file on disk → it verifies and seeds.
        var seederDir = Path.Combine(_root, "seeder");
        Directory.CreateDirectory(seederDir);
        await File.WriteAllBytesAsync(Path.Combine(seederDir, fileName), original);

        _seeder = TorrentClientBuilder.CreateDefaultBuilder()
            .UsePort(SeederPort)
            .Build();
        _seeder.Add(metainfo, seederDir).Start();

        // Give the seeder a moment to verify its pieces and start listening.
        await Task.Delay(500);

        // 3. Leecher starts empty; its tracker is stubbed to return the seeder's endpoint.
        var leecherDir = Path.Combine(_root, "leecher");
        _leecher = TorrentClientBuilder.CreateDefaultBuilder()
            .UsePort(LeecherPort)
            .ConfigureServices(s =>
                s.AddSingleton<ITrackerClientFactory>(new StubTrackerClientFactory(SeederPort)))
            .Build();
        var download = _leecher.Add(metainfo, leecherDir);
        download.Start();

        // 4. Wait for the download to complete (fires on full, hash-verified download).
        await download.WaitForDownloadCompletionAsync(TimeSpan.FromSeconds(40));
        Assert.Equal(DownloadState.Completed, download.State);

        // Close the leecher first so its file handles are flushed and released before we read.
        _leecher.Dispose();
        _leecher = null;

        // 5. The downloaded file must match the original byte for byte.
        var downloadedPath = Path.Combine(leecherDir, fileName);
        Assert.True(File.Exists(downloadedPath), "downloaded file should exist");

        var downloaded = await File.ReadAllBytesAsync(downloadedPath);
        Assert.Equal(original.Length, downloaded.Length);
        Assert.Equal(SHA1.HashData(original), SHA1.HashData(downloaded));
    }

    // ─── Stub tracker: always returns the seeder as the only peer ────────────────

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
