namespace TorrentCs.Extensions.ExtensionProtocol;

/// <summary>
/// A message belonging to a BEP 10 extension (e.g. ut_pex, ut_metadata). Each extension defines its
/// own message types; the <see cref="ExtensionProtocolModule"/> handles framing and dispatch so the
/// extension only deals with the typed message, never the raw wire format.
/// </summary>
public interface IExtensionProtocolMessage
{
    /// <summary>The name advertised in the extended handshake's <c>m</c> dictionary (e.g. "ut_pex").</summary>
    string MessageType { get; }

    /// <summary>Serializes the message payload (everything after the extended message id byte).</summary>
    byte[] Serialize();

    /// <summary>Populates this message from a received payload.</summary>
    void Deserialize(byte[] data);
}
