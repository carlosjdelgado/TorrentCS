namespace TorrentCs.Tracker;

public interface ITrackerDetails
{
    Uri Uri { get; }
    int Peers { get; }
    DateTime LastAnnounce { get; }
    string Type { get; }
}
