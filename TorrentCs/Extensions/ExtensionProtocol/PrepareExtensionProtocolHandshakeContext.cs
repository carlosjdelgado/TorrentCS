using BencodeNET.Objects;

namespace TorrentCs.Extensions.ExtensionProtocol;

internal sealed class PrepareExtensionProtocolHandshakeContext : IPrepareExtensionProtocolHandshakeContext
{
    public PrepareExtensionProtocolHandshakeContext(BDictionary handshakeContent) =>
        HandshakeContent = handshakeContent;

    public BDictionary HandshakeContent { get; }
}
