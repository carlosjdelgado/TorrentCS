namespace TorrentCs.Modularity;

public interface IModule
{
    void OnPrepareHandshake(IPrepareHandshakeContext context);
    void OnPeerConnected(IPeerContext context);
    void OnMessageReceived(IMessageReceivedContext context);
}
