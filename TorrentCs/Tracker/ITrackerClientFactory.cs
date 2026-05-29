namespace TorrentCs.Tracker;

public interface ITrackerClientFactory
{
    ITracker? CreateTrackerClient(Uri trackerUri);
}
