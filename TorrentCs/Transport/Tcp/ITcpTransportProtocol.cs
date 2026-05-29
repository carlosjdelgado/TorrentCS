using System.Net;

namespace TorrentCs.Transport.Tcp;

public interface ITcpTransportProtocol : ITransportProtocol
{
    TcpTransportStream CreateTransportStream(IPAddress remoteAddress, int port);
}
