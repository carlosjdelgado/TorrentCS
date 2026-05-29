using System.Net;

namespace TorrentCs.Transport.Tcp;

public class LocalTcpConnectionOptions
{
    public int Port { get; set; }
    public IPAddress PublicAddress { get; set; } = IPAddress.Loopback;
    public IPAddress BindAddress { get; set; } = IPAddress.Any;
}
