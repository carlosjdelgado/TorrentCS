namespace TorrentCs.Transport;

/// <summary>A no-op port forwarder — UPnP/NAT-PMP disabled.</summary>
public sealed class NullPortForwarding : IPortForwarding
{
    public void MapPort(int port) { }

    public void Dispose() { }
}
