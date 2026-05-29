namespace TorrentCs.Transport;

/// <summary>
/// Forwards the local listen port on the home router so peers can establish incoming connections.
/// </summary>
public interface IPortForwarding : IDisposable
{
    /// <summary>
    /// Begins forwarding the given TCP port on the router. Best-effort and asynchronous: router
    /// discovery happens in the background and the mapping is created once a device is found.
    /// </summary>
    void MapPort(int port);
}
