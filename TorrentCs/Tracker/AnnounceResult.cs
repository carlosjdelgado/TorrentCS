using TorrentCs.Transport;

namespace TorrentCs.Tracker;

public class AnnounceResult
{
    public const int DefaultInterval = 1800;

    public AnnounceResult(IEnumerable<ITransportStream> peers, int interval = DefaultInterval)
    {
        Peers = peers.ToArray();
        Interval = interval;
    }

    public IReadOnlyList<ITransportStream> Peers { get; }

    /// <summary>Seconds the client should wait before re-announcing to the tracker.</summary>
    public int Interval { get; }
}
