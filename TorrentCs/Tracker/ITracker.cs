namespace TorrentCs.Tracker;

public interface ITracker
{
    string Type { get; }

    Task<AnnounceResult> Announce(AnnounceRequest request);
}
