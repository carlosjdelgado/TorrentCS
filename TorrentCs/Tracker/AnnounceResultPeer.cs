using System.Net;

namespace TorrentCs.Tracker;

public class AnnounceResultPeer
{
    public AnnounceResultPeer(IPAddress ipAddress, int port)
    {
        IPAddress = ipAddress;
        Port = port;
    }

    public IPAddress IPAddress { get; }
    public int Port { get; }
}
