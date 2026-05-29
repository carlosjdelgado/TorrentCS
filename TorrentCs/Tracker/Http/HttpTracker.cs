using System.Net;
using System.Text;
using BencodeNET.Objects;
using BencodeNET.Parsing;
using Microsoft.Extensions.Logging;
using TorrentCs.Transport.Tcp;

namespace TorrentCs.Tracker.Http;

public class HttpTracker : ITracker
{
    private readonly ILogger<HttpTracker> _logger;
    private readonly LocalTcpConnectionOptions _options;
    private readonly Uri _trackerUri;

    public HttpTracker(ILogger<HttpTracker> logger, LocalTcpConnectionOptions options, Uri trackerUri)
    {
        _logger = logger;
        _options = options;
        _trackerUri = trackerUri;
    }

    public string Type => "HTTP";

    public async Task<AnnounceResult> Announce(AnnounceRequest request)
    {
        var url = BuildRequestUrl(request);
        _logger.LogDebug("HTTP announce to {Url}", _trackerUri);

        try
        {
            var response = await HttpGet(url);
            var dict = new BencodeParser().Parse<BDictionary>(response);

            if (dict.ContainsKey("failure reason"))
            {
                _logger.LogWarning("Tracker {Uri} returned failure: {Reason}",
                    _trackerUri, ((BString)dict["failure reason"]).ToString());
                return new AnnounceResult([]);
            }

            int interval = dict.ContainsKey("interval")
                ? (int)((BNumber)dict["interval"]).Value
                : AnnounceResult.DefaultInterval;
            var peers = ParsePeers(dict).ToList();

            _logger.LogDebug("Tracker {Uri} returned {Count} peers (interval {Interval}s)",
                _trackerUri, peers.Count, interval);
            return new AnnounceResult(peers, interval);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "HTTP announce failed for {Uri}", _trackerUri);
            return new AnnounceResult([]);
        }
    }

    protected virtual async Task<Stream> HttpGet(string url)
    {
        using var client = new HttpClient();
        var response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStreamAsync();
    }

    private string BuildRequestUrl(AnnounceRequest request)
    {
        var sb = new StringBuilder(_trackerUri.ToString());
        sb.Append(_trackerUri.Query.Length > 0 ? '&' : '?');
        sb.Append("info_hash=").Append(PercentEncode(request.InfoHash.Value));
        sb.Append("&peer_id=").Append(PercentEncode(request.PeerId));
        sb.Append("&port=").Append(_options.Port);
        sb.Append("&uploaded=").Append(request.Uploaded);
        sb.Append("&downloaded=").Append(request.Downloaded);
        sb.Append("&left=").Append(request.Remaining);
        sb.Append("&compact=1");
        sb.Append("&numwant=").Append(request.NumWant);
        if (request.Event != TrackerEvent.None)
            sb.Append("&event=").Append(request.Event.ToString().ToLowerInvariant());
        return sb.ToString();
    }

    private IEnumerable<TcpTransportStream> ParsePeers(BDictionary dict)
    {
        if (!dict.ContainsKey("peers"))
            return [];

        var peersObj = dict["peers"];

        if (peersObj is BString compactPeers)
            return ParseCompactPeers(compactPeers.Value.ToArray());

        if (peersObj is BList peerList)
            return ParseDictPeers(peerList);

        return [];
    }

    private IEnumerable<TcpTransportStream> ParseCompactPeers(byte[] data)
    {
        for (int i = 0; i + 5 < data.Length; i += 6)
        {
            var ip = new IPAddress(data[i..(i + 4)]);
            int port = (data[i + 4] << 8) | data[i + 5];
            yield return new TcpTransportStream(_options.BindAddress, ip, port);
        }
    }

    private IEnumerable<TcpTransportStream> ParseDictPeers(BList peerList)
    {
        foreach (var entry in peerList.Cast<BDictionary>())
        {
            if (!entry.ContainsKey("ip") || !entry.ContainsKey("port"))
                continue;

            var ipStr = ((BString)entry["ip"]).ToString();
            var port = (int)((BNumber)entry["port"]).Value;

            if (!IPAddress.TryParse(ipStr, out var ip))
                continue;

            yield return new TcpTransportStream(_options.BindAddress, ip, port);
        }
    }

    private static string PercentEncode(byte[] bytes)
    {
        var sb = new StringBuilder(bytes.Length * 3);
        foreach (var b in bytes)
        {
            if ((b >= 'a' && b <= 'z') || (b >= 'A' && b <= 'Z') ||
                (b >= '0' && b <= '9') || b == '-' || b == '_' || b == '.' || b == '~')
                sb.Append((char)b);
            else
                sb.Append('%').Append(b.ToString("X2"));
        }
        return sb.ToString();
    }
}
