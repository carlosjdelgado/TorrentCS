using TorrentCs.Transport;

namespace TorrentCs.Application;

public class AcceptPeerConnectionEventArgs
{
    public AcceptPeerConnectionEventArgs(ITransportStream stream)
    {
        Stream = stream;
    }

    public ITransportStream Stream { get; }
    public bool Accepted { get; private set; }

    public void Accept() => Accepted = true;
}
