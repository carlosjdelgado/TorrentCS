namespace TorrentCs.Extensions.ExtensionProtocol;

/// <summary>
/// Context for a received extension message: the peer context plus the deserialized message.
/// </summary>
public interface IExtensionProtocolMessageReceivedContext : IExtensionProtocolPeerContext
{
    /// <summary>The received message, already deserialized into its concrete type.</summary>
    IExtensionProtocolMessage Message { get; }
}
