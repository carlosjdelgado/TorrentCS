using System.Net;
using System.Net.Sockets;
using System.Text;
using BencodeNET.Objects;
using BencodeNET.Parsing;
using Microsoft.Extensions.Logging;
using TorrentCs.Modularity;
using TorrentCs.Transport;
using TorrentCs.Transport.Tcp;

namespace TorrentCs.Application.BitTorrent.ExtensionModule;

/// <summary>
/// Peer Exchange (BEP 11, "ut_pex"): gossips peers with the peers we are connected to. Incoming PEX
/// messages feed newly-advertised peers back into the swarm; on each tick we send a peer the compact
/// list of the other peers we are connected to.
/// </summary>
public class PexExtension : IBitTorrentExtension
{
    private readonly ILogger<PexExtension> _logger;
    private readonly LocalTcpConnectionOptions _options;

    public PexExtension(ILogger<PexExtension> logger, LocalTcpConnectionOptions options)
    {
        _logger = logger;
        _options = options;
    }

    public string Name => "ut_pex";

    public void OnMessageReceived(IPeerContext context, byte[] data)
    {
        try
        {
            var dict = new BencodeParser().Parse<BDictionary>(new MemoryStream(data));
            if (dict.ContainsKey("added") && dict["added"] is BString added)
            {
                var peers = ParseCompactPeers(added.Value.ToArray());
                if (peers.Count > 0)
                {
                    _logger.LogDebug("PEX: discovered {Count} peers from {Address}",
                        peers.Count, context.Peer.Address);
                    context.PeersAvailable(peers);
                }
            }
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to parse PEX message");
        }
    }

    public void OnTick(IPeerContext context)
    {
        var peer = (BitTorrentPeer)context.Peer;

        // The peer must have advertised ut_pex in its extended handshake; we address it with the id
        // it assigned to ut_pex in its own 'm' dictionary.
        if (!peer.Values.TryGetValue(ExtensionProtocolModule.PeerExtensionsKey, out var value) ||
            value is not Dictionary<string, byte> peerExtensions ||
            !peerExtensions.TryGetValue(Name, out var peerPexId))
            return;

        var added = new List<byte>();
        foreach (var other in context.Peers)
        {
            if (ReferenceEquals(other, peer)) continue; // don't gossip a peer back to itself
            if (TryGetCompactPeer(other, out var compact))
                added.AddRange(compact);
        }

        if (added.Count == 0) return;

        var message = new BDictionary { ["added"] = new BString(added.ToArray(), Encoding.Latin1) };
        using var ms = new MemoryStream();
        ms.WriteByte(peerPexId);
        message.EncodeTo(ms);
        context.SendMessage(ExtensionProtocolModule.ExtendedMessageId, ms.ToArray());
    }

    private List<ITransportStream> ParseCompactPeers(byte[] bytes)
    {
        var peers = new List<ITransportStream>();
        for (int i = 0; i + 5 < bytes.Length; i += 6)
        {
            var ip = new IPAddress(bytes[i..(i + 4)]);
            int port = (bytes[i + 4] << 8) | bytes[i + 5];
            peers.Add(new TcpTransportStream(_options.BindAddress, ip, port));
        }
        return peers;
    }

    private static bool TryGetCompactPeer(IPeer peer, out byte[] compact)
    {
        compact = [];
        if (!IPEndPoint.TryParse(peer.Address, out var endpoint) ||
            endpoint.AddressFamily != AddressFamily.InterNetwork)
            return false;

        // Prefer the peer's advertised listen port (BEP 10 "p") over its connection endpoint port,
        // which for an incoming connection is the remote's ephemeral port and not reachable.
        int port = endpoint.Port;
        if (peer is BitTorrentPeer bt &&
            bt.Values.TryGetValue(ExtensionProtocolModule.PeerListenPortKey, out var value) &&
            value is int listenPort)
            port = listenPort;

        var ip = endpoint.Address.GetAddressBytes(); // 4 bytes for IPv4
        compact = [ip[0], ip[1], ip[2], ip[3], (byte)(port >> 8), (byte)port];
        return true;
    }
}
