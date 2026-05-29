using System.Net;
using System.Net.Sockets;
using Microsoft.Extensions.Logging;
using TorrentCs.Application.BitTorrent;
using TorrentCs.Extensions.ExtensionProtocol;
using TorrentCs.Transport;
using TorrentCs.Transport.Tcp;

namespace TorrentCs.Extensions.PeerExchange;

/// <summary>
/// Peer Exchange (BEP 11, "ut_pex"). On receiving a PEX message we feed its peers back into the
/// swarm; reactively (throttled to once per minute) we reply with the peers added to and dropped from
/// our swarm since the last message, mirroring how established clients gossip peers.
/// </summary>
public class PeerExchangeMessageHandler : IExtensionProtocolMessageHandler
{
    private static readonly TimeSpan GossipInterval = TimeSpan.FromMinutes(1);

    private readonly ILogger<PeerExchangeMessageHandler> _logger;
    private readonly ITcpTransportProtocol _tcpTransportProtocol;

    public PeerExchangeMessageHandler(
        ILogger<PeerExchangeMessageHandler> logger, ITcpTransportProtocol tcpTransportProtocol)
    {
        _logger = logger;
        _tcpTransportProtocol = tcpTransportProtocol;
    }

    public IReadOnlyDictionary<string, Func<IExtensionProtocolMessage>> SupportedMessageTypes { get; } =
        new Dictionary<string, Func<IExtensionProtocolMessage>>
        {
            [PeerExchangeMessage.Type] = () => new PeerExchangeMessage(),
        };

    public void MessageReceived(IExtensionProtocolMessageReceivedContext context)
    {
        var message = (PeerExchangeMessage)context.Message;

        if (message.Added.Count > 0)
        {
            _logger.LogDebug("PEX: discovered {Count} peers from {Address}",
                message.Added.Count, context.Peer.Address);
            context.PeersAvailable(message.Added.Select(CreateTransportStream));
        }

        GossipPeers(context);
    }

    private ITransportStream CreateTransportStream(IPEndPoint endpoint) =>
        _tcpTransportProtocol.CreateTransportStream(endpoint.Address, endpoint.Port);

    // Reply with the peers added/dropped since we last gossiped to this peer, no more than once per
    // GossipInterval. The recipient is excluded so we never gossip a peer back to itself.
    private void GossipPeers(IExtensionProtocolMessageReceivedContext context)
    {
        var metadata = context.GetValue<PeerExchangeMetadata>(PeerExchangeMetadata.Key) ?? new PeerExchangeMetadata();
        if (DateTime.UtcNow - metadata.LastMessageDate < GossipInterval)
            return;

        var currentPeers = context.Peers
            .Where(p => !ReferenceEquals(p, context.Peer) && p.Address != context.Peer.Address)
            .ToList();
        var currentAddresses = currentPeers.Select(p => p.Address).ToList();

        var added = currentPeers
            .Where(p => !metadata.ConnectedPeersSnapshot.Contains(p.Address))
            .Select(p => ToEndpoint(context, p))
            .OfType<IPEndPoint>()
            .ToList();
        var dropped = metadata.ConnectedPeersSnapshot
            .Where(address => !currentAddresses.Contains(address))
            .Select(ParseEndpoint)
            .OfType<IPEndPoint>()
            .ToList();

        if (added.Count == 0 && dropped.Count == 0)
            return;

        context.SendMessage(new PeerExchangeMessage { Added = added, Dropped = dropped });

        metadata.LastMessageDate = DateTime.UtcNow;
        metadata.ConnectedPeersSnapshot = currentAddresses;
        context.SetValue(PeerExchangeMetadata.Key, metadata);
    }

    // The peer's connection endpoint, but preferring its advertised listen port (BEP 10 "p") so the
    // peer is reported with a port others can actually connect to.
    private static IPEndPoint? ToEndpoint(IExtensionProtocolPeerContext context, IPeer peer)
    {
        if (!IPEndPoint.TryParse(peer.Address, out var endpoint) ||
            endpoint.AddressFamily != AddressFamily.InterNetwork)
            return null;

        int port = context.GetListenPort(peer) ?? endpoint.Port;
        return new IPEndPoint(endpoint.Address, port);
    }

    private static IPEndPoint? ParseEndpoint(string address) =>
        IPEndPoint.TryParse(address, out var endpoint) &&
        endpoint.AddressFamily == AddressFamily.InterNetwork
            ? endpoint
            : null;
}
