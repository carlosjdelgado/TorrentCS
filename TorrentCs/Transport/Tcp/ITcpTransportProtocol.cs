using System.Net;

namespace TorrentCs.Transport.Tcp;

public interface ITcpTransportProtocol : ITransportProtocol
{
    /// <summary>The port the protocol is actually listening on (assigned after <c>Start</c>).</summary>
    int Port { get; }

    TcpTransportStream CreateTransportStream(IPAddress remoteAddress, int port);
}
