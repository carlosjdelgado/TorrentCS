namespace TorrentCs.Tracker;

/// <summary>
/// The event reported to a tracker on announce, per BEP 3.
/// </summary>
public enum TrackerEvent
{
    None,
    Started,
    Stopped,
    Completed,
}
