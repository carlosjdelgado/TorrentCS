using TorrentCs.Transport;

namespace TorrentCs.Application.BitTorrent;

public class TransportStreamAddressEqualityComparer : IEqualityComparer<ITransportStream>
{
    public static readonly TransportStreamAddressEqualityComparer Instance = new();

    public bool Equals(ITransportStream? x, ITransportStream? y)
    {
        if (x is null) return y is null;
        if (y is null) return false;
        return x.Address?.Equals(y.Address) ?? y.Address is null;
    }

    public int GetHashCode(ITransportStream obj) => obj.Address?.GetHashCode() ?? 0;
}
