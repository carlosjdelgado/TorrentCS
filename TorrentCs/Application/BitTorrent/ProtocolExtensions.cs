namespace TorrentCs.Application.BitTorrent;

[Flags]
public enum ProtocolExtension
{
    None = 0,
    FastPeers = 1,        // BEP 6
    Dht = 2,              // BEP 5
    ExtensionProtocol = 4 // BEP 10
}

public static class ProtocolExtensions
{
    public static ProtocolExtension DetermineSupportedProtocolExtensions(byte[] reservedBytes)
    {
        var extensions = ProtocolExtension.None;

        if (reservedBytes.Length < 8) return extensions;

        if ((reservedBytes[7] & 0x01) != 0) extensions |= ProtocolExtension.Dht;
        if ((reservedBytes[7] & 0x04) != 0) extensions |= ProtocolExtension.FastPeers;
        if ((reservedBytes[5] & 0x10) != 0) extensions |= ProtocolExtension.ExtensionProtocol;

        return extensions;
    }
}
