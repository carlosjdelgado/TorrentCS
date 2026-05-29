using BencodeNET.Objects;
using TorrentCs.Data;

namespace TorrentCs.Extensions.ExtensionProtocol;

/// <summary>
/// Lets an extension contribute fields to the outgoing extended handshake before it is sent.
/// </summary>
public interface IPrepareExtensionProtocolHandshakeContext
{
    /// <summary>The handshake dictionary being built. Extensions may add their own keys.</summary>
    BDictionary HandshakeContent { get; }

    /// <summary>The torrent's metainfo, so an extension can advertise metainfo-derived fields.</summary>
    Metainfo Metainfo { get; }
}
