using System.Net.Sockets;

namespace TorrentCs.Transport;

public class TransportConnectionEventArgs
{
    public TransportConnectionEventArgs(TcpClient client)
    {
        Client = client;
    }

    public TcpClient Client { get; }
}
