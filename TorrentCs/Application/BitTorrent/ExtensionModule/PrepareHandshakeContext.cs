using TorrentCs.Modularity;

namespace TorrentCs.Application.BitTorrent.ExtensionModule;

public sealed class PrepareHandshakeContext : IPrepareHandshakeContext
{
    public PrepareHandshakeContext(byte[] reservedBytes) => ReservedBytes = reservedBytes;

    public byte[] ReservedBytes { get; }
}
