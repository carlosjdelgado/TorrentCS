using Microsoft.Extensions.Logging;
using TorrentCs.Application.BitTorrent.ExtensionModule;
using TorrentCs.Application.BitTorrent.Messages;
using TorrentCs.Data;
using TorrentCs.Data.Pieces;
using TorrentCs.Modularity;
using TorrentCs.Transport;

namespace TorrentCs.Application.BitTorrent;

public class BitTorrentApplicationProtocol : IApplicationProtocol, IPeerMessageHandler, ITorrentContext
{
    private const int MaxPeers = 125;

    private readonly ILogger<BitTorrentApplicationProtocol> _logger;
    private readonly IReadOnlyList<IModule> _modules;
    private readonly PeerId _localPeerId;
    private readonly List<BitTorrentPeer> _peers = new();
    private readonly List<ITransportStream> _availablePeers = new();
    private readonly List<ITransportStream> _connectingPeers = new();
    private readonly object _peersLock = new();

    public BitTorrentApplicationProtocol(
        ILogger<BitTorrentApplicationProtocol> logger,
        Metainfo metainfo,
        IPieceDataHandler dataHandler,
        IBlockRequests blockRequests,
        IEnumerable<IModule> modules,
        PeerId localPeerId)
    {
        _logger = logger;
        Metainfo = metainfo;
        DataHandler = dataHandler;
        BlockRequests = blockRequests;
        _modules = modules.ToList().AsReadOnly();
        _localPeerId = localPeerId;
    }

    public event Action? DownloadCompleted;

    public Metainfo Metainfo { get; }
    public IPieceDataHandler DataHandler { get; }
    public IBlockRequests BlockRequests { get; }
    public long Uploaded { get; private set; }

    public IReadOnlyCollection<object> Peers
    {
        get { lock (_peersLock) return _peers.Cast<object>().ToList().AsReadOnly(); }
    }

    public IReadOnlyCollection<ITransportStream> AvailablePeers
    {
        get { lock (_peersLock) return _availablePeers.ToList(); }
    }

    public IReadOnlyCollection<ITransportStream> ConnectingPeers
    {
        get { lock (_peersLock) return _connectingPeers.ToList(); }
    }

    // ITorrentContext
    IReadOnlyCollection<IPeer> ITorrentContext.Peers
    {
        get { lock (_peersLock) return _peers.Cast<IPeer>().ToList().AsReadOnly(); }
    }

    void ITorrentContext.PeersAvailable(IEnumerable<ITransportStream> peers) => PeersAvailable(peers);

    public void PeersAvailable(IEnumerable<ITransportStream> peers)
    {
        lock (_peersLock)
        {
            // Register newly-discovered peers as available. The download pipeline is the single
            // place that opens connections (respecting the connection limit), so we do NOT connect
            // here — doing both would open two connections to the same peer over one socket.
            var known = new HashSet<string>(StringComparer.Ordinal);
            known.UnionWith(_availablePeers.Select(p => p.DisplayAddress));
            known.UnionWith(_connectingPeers.Select(p => p.DisplayAddress));
            known.UnionWith(_peers.Select(p => p.Address));

            foreach (var peer in peers)
            {
                if (_peers.Count + _connectingPeers.Count + _availablePeers.Count >= MaxPeers) break;
                if (known.Add(peer.DisplayAddress))
                    _availablePeers.Add(peer);
            }
        }
    }

    public void ConnectToPeer(ITransportStream stream) => _ = ConnectToPeerAsync(stream);

    public void AcceptConnection(AcceptPeerConnectionEventArgs args)
        => _ = AcceptConnectionAsync(args.Stream);

    public void PieceCompleted(Piece piece)
    {
        List<BitTorrentPeer> snapshot;
        lock (_peersLock) snapshot = [.. _peers];

        using var ms = new MemoryStream(4);
        new BigEndianBinaryWriter(ms).Write(piece.Index);
        var body = ms.ToArray();

        foreach (var peer in snapshot)
            peer.SendMessage(HaveMessage.MessageID, body);

        if (DataHandler.Metainfo.Pieces.All(p => DataHandler.CompletedPieces.Contains(p)))
            DownloadCompleted?.Invoke();
    }

    public void PieceCorrupted(Piece piece) =>
        _logger.LogWarning("Piece {Index} corrupted, will re-download", piece.Index);

    public void MessageReceived(byte messageId, int length, BinaryReader reader, BitTorrentPeer peer)
    {
        var peerCtx = BuildPeerContext(peer);
        var msgCtx = new MessageReceivedContext(peerCtx, messageId, length, reader);

        lock (_peersLock)
        {
            if (!_peers.Contains(peer)) return;
        }

        foreach (var module in _modules)
            module.OnMessageReceived(msgCtx);
    }

    public void PeerDisconnected(BitTorrentPeer peer)
    {
        lock (_peersLock) _peers.Remove(peer);
        _logger.LogDebug("Peer disconnected: {Address}", peer.Address);
    }

    private async Task ConnectToPeerAsync(ITransportStream stream)
    {
        lock (_peersLock)
        {
            if (_connectingPeers.Contains(stream)) return; // already connecting
            _availablePeers.Remove(stream);
            _connectingPeers.Add(stream);
        }
        try
        {
            await stream.Connect();
            var peer = new BitTorrentPeer(stream, Metainfo);
            await peer.PerformHandshakeAsync(Metainfo, _localPeerId, isInitiator: true);
            PeerConnected(peer, stream);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to connect to {Address}", stream.DisplayAddress);
        }
        finally
        {
            lock (_peersLock) _connectingPeers.Remove(stream);
        }
    }

    private async Task AcceptConnectionAsync(ITransportStream stream)
    {
        try
        {
            var peer = new BitTorrentPeer(stream, Metainfo);
            await peer.PerformHandshakeAsync(Metainfo, _localPeerId, isInitiator: false);
            PeerConnected(peer, stream);
        }
        catch (Exception ex)
        {
            _logger.LogDebug(ex, "Failed to accept connection from {Address}", stream.DisplayAddress);
        }
    }

    private void PeerConnected(BitTorrentPeer peer, ITransportStream stream)
    {
        peer.SetHandler(this);
        lock (_peersLock) _peers.Add(peer);

        var ctx = BuildPeerContext(peer);
        foreach (var module in _modules)
            module.OnPeerConnected(ctx);

        _ = peer.ReceiveMessagesAsync(Metainfo, CancellationToken.None);
        _logger.LogDebug("Peer connected: {Address}", peer.Address);
    }

    private PeerContext BuildPeerContext(BitTorrentPeer peer)
    {
        var registeredIds = new HashSet<byte>();
        return new PeerContext(peer, peer.Values, this,
            id => registeredIds.Add(id));
    }
}
