using System.Net;
using System.Text;
using BencodeNET.Objects;
using Microsoft.Extensions.Logging.Abstractions;
using TorrentCs.Data;
using TorrentCs.Tracker;
using TorrentCs.Tracker.Http;
using TorrentCs.Transport.Tcp;

namespace TorrentCs.Tests.Tracker.Http;

public class HttpTrackerTests
{
    private static AnnounceRequest MakeRequest() =>
        new(new byte[20], remaining: 0, downloaded: 1024, uploaded: 512,
            new Sha1Hash(new byte[20]));

    private static LocalTcpConnectionOptions MakeOptions() => new()
    {
        Port = 6881,
        PublicAddress = IPAddress.Loopback,
        BindAddress = IPAddress.Loopback,
    };

    [Fact]
    public void Type_IsHttp()
    {
        var tracker = new StubHttpTracker(MakeOptions(),
            new Uri("http://tracker.example.com/announce"), Stream.Null);
        Assert.Equal("HTTP", tracker.Type);
    }

    [Fact]
    public async Task Announce_CompactResponse_ReturnsPeers()
    {
        // Compact format: 4 bytes IP + 2 bytes port per peer
        // Peer: 127.0.0.1:6969
        var peerBytes = new byte[] { 127, 0, 0, 1, 0x1B, 0x39 };
        var response = BuildCompactResponse(peerBytes);

        var tracker = new StubHttpTracker(MakeOptions(),
            new Uri("http://tracker.example.com/announce"), response);

        var result = await tracker.Announce(MakeRequest());

        // RemoteEndPoint is null until Connect() — just verify a stream was created
        Assert.Single(result.Peers);
        Assert.IsType<TcpTransportStream>(result.Peers[0]);
    }

    [Fact]
    public async Task Announce_DictResponse_ReturnsPeers()
    {
        var response = BuildDictResponse("127.0.0.1", 6881);

        var tracker = new StubHttpTracker(MakeOptions(),
            new Uri("http://tracker.example.com/announce"), response);

        var result = await tracker.Announce(MakeRequest());

        Assert.Single(result.Peers);
    }

    [Fact]
    public async Task Announce_EmptyPeers_ReturnsEmptyResult()
    {
        var response = BuildCompactResponse([]);

        var tracker = new StubHttpTracker(MakeOptions(),
            new Uri("http://tracker.example.com/announce"), response);

        var result = await tracker.Announce(MakeRequest());
        Assert.Empty(result.Peers);
    }

    [Fact]
    public async Task Announce_NetworkError_ReturnsEmptyResult()
    {
        var tracker = new FailingHttpTracker(MakeOptions(),
            new Uri("http://tracker.example.com/announce"));

        var result = await tracker.Announce(MakeRequest());
        Assert.Empty(result.Peers);
    }

    [Fact]
    public async Task Announce_RequestUrl_IncludesNumWant()
    {
        var tracker = new StubHttpTracker(MakeOptions(),
            new Uri("http://tracker.example.com/announce"), BuildCompactResponse([]));

        await tracker.Announce(MakeRequest());

        Assert.Contains("numwant=200", tracker.LastUrl);
    }

    [Fact]
    public async Task Announce_StartedEvent_AppearsInUrl()
    {
        var tracker = new StubHttpTracker(MakeOptions(),
            new Uri("http://tracker.example.com/announce"), BuildCompactResponse([]));

        var request = new AnnounceRequest(new byte[20], 0, 0, 0,
            new Sha1Hash(new byte[20]), TrackerEvent.Started);
        await tracker.Announce(request);

        Assert.Contains("event=started", tracker.LastUrl);
    }

    [Fact]
    public async Task Announce_NoneEvent_OmitsEventFromUrl()
    {
        var tracker = new StubHttpTracker(MakeOptions(),
            new Uri("http://tracker.example.com/announce"), BuildCompactResponse([]));

        await tracker.Announce(MakeRequest()); // default event = None

        Assert.DoesNotContain("event=", tracker.LastUrl);
    }

    [Fact]
    public async Task Announce_ParsesInterval()
    {
        var dict = new BDictionary
        {
            ["interval"] = new BNumber(900),
            ["peers"] = new BString(Array.Empty<byte>(), Encoding.Latin1),
        };
        var ms = new MemoryStream();
        dict.EncodeTo(ms);
        ms.Seek(0, SeekOrigin.Begin);

        var tracker = new StubHttpTracker(MakeOptions(),
            new Uri("http://tracker.example.com/announce"), ms);

        var result = await tracker.Announce(MakeRequest());
        Assert.Equal(900, result.Interval);
    }

    [Fact]
    public async Task Announce_FailureReason_ReturnsEmptyResult()
    {
        var dict = new BDictionary
        {
            ["failure reason"] = new BString("torrent not registered", Encoding.UTF8),
        };
        var ms = new MemoryStream();
        dict.EncodeTo(ms);
        ms.Seek(0, SeekOrigin.Begin);

        var tracker = new StubHttpTracker(MakeOptions(),
            new Uri("http://tracker.example.com/announce"), ms);

        var result = await tracker.Announce(MakeRequest());
        Assert.Empty(result.Peers);
    }

    private static Stream BuildCompactResponse(byte[] peerBytes)
    {
        var dict = new BDictionary
        {
            ["interval"] = new BNumber(1800),
            ["peers"] = new BString(peerBytes, System.Text.Encoding.Latin1),
        };
        var ms = new MemoryStream();
        dict.EncodeTo(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    private static Stream BuildDictResponse(string ip, int port)
    {
        var peerEntry = new BDictionary
        {
            ["ip"] = new BString(ip, Encoding.UTF8),
            ["port"] = new BNumber(port),
            ["peer id"] = new BString(new byte[20], Encoding.Latin1),
        };
        var dict = new BDictionary
        {
            ["interval"] = new BNumber(1800),
            ["peers"] = new BList { peerEntry },
        };
        var ms = new MemoryStream();
        dict.EncodeTo(ms);
        ms.Seek(0, SeekOrigin.Begin);
        return ms;
    }

    // ─── Test doubles ────────────────────────────────────────────────────────

    private sealed class StubHttpTracker(LocalTcpConnectionOptions options, Uri uri, Stream stubResponse)
        : HttpTracker(NullLogger<HttpTracker>.Instance, options, uri)
    {
        public string? LastUrl { get; private set; }

        protected override Task<Stream> HttpGet(string url)
        {
            LastUrl = url;
            return Task.FromResult(stubResponse);
        }
    }

    private sealed class FailingHttpTracker(LocalTcpConnectionOptions options, Uri uri)
        : HttpTracker(NullLogger<HttpTracker>.Instance, options, uri)
    {
        protected override Task<Stream> HttpGet(string url) =>
            throw new HttpRequestException("Simulated network error");
    }
}
