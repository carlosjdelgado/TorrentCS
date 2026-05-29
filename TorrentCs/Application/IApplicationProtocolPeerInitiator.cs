using TorrentCs.Transport;

namespace TorrentCs.Application;

public interface IApplicationProtocolPeerInitiator
{
    void OnApplicationProtocolAdded(IApplicationProtocol protocol);
    void OnApplicationProtocolRemoved(IApplicationProtocol protocol);
    Task AcceptIncomingConnection(ITransportStream stream);
    Task InitiateOutgoingConnection(ITransportStream stream, IApplicationProtocol protocol);
}
