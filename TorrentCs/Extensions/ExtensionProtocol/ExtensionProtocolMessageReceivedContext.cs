using TorrentCs.Modularity;

namespace TorrentCs.Extensions.ExtensionProtocol;

internal sealed class ExtensionProtocolMessageReceivedContext
    : ExtensionProtocolPeerContext, IExtensionProtocolMessageReceivedContext
{
    public ExtensionProtocolMessageReceivedContext(
        IPeerContext inner,
        IExtensionProtocolMessage message,
        Action<IExtensionProtocolMessage> sendMessage)
        : base(inner, sendMessage) => Message = message;

    public IExtensionProtocolMessage Message { get; }
}
