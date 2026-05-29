namespace TorrentCs.Modularity;

public interface IModule
{
    void OnPrepareHandshake(IPrepareHandshakeContext context);
    void OnPeerConnected(IPeerContext context);
    void OnMessageReceived(IMessageReceivedContext context);

    /// <summary>
    /// Periodic per-peer maintenance, invoked roughly every minute for each connected peer.
    /// </summary>
    void OnTick(IPeerContext context) { }
}
