using TorrentCs.Modularity;

namespace TorrentCs.Application.BitTorrent.ExtensionModule;

/// <summary>
/// A BEP 10 extension (e.g. ut_pex, ut_metadata). The <see cref="ExtensionProtocolModule"/>
/// advertises it in the extended handshake and dispatches its messages.
/// </summary>
public interface IBitTorrentExtension
{
    /// <summary>The name advertised in the extended handshake's <c>m</c> dictionary.</summary>
    string Name { get; }

    /// <summary>Called when the peer sends a message addressed to this extension.</summary>
    void OnMessageReceived(IPeerContext context, byte[] data);
}
