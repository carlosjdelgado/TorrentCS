using BencodeNET.Objects;
using TorrentCs.Data;

namespace TorrentCs.Extensions.ExtensionProtocol;

internal sealed class PrepareExtensionProtocolHandshakeContext : IPrepareExtensionProtocolHandshakeContext
{
    public PrepareExtensionProtocolHandshakeContext(BDictionary handshakeContent, Metainfo metainfo)
    {
        HandshakeContent = handshakeContent;
        Metainfo = metainfo;
    }

    public BDictionary HandshakeContent { get; }

    public Metainfo Metainfo { get; }
}
