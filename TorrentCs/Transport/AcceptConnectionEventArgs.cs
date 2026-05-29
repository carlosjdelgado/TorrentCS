namespace TorrentCs.Transport;

public class AcceptConnectionEventArgs
{
    public AcceptConnectionEventArgs(ITransportStream transportStream, Action accept)
    {
        TransportStream = transportStream;
        Accept = accept;
    }

    public ITransportStream TransportStream { get; }
    public Action Accept { get; }
}
