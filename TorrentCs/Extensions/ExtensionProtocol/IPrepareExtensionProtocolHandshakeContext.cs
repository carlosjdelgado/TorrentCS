using BencodeNET.Objects;

namespace TorrentCs.Extensions.ExtensionProtocol;

/// <summary>
/// Lets an extension contribute fields to the outgoing extended handshake before it is sent.
/// </summary>
public interface IPrepareExtensionProtocolHandshakeContext
{
    /// <summary>The handshake dictionary being built. Extensions may add their own keys.</summary>
    BDictionary HandshakeContent { get; }
}
