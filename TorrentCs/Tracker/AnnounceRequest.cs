using TorrentCs.Data;

namespace TorrentCs.Tracker;

public class AnnounceRequest
{
    public AnnounceRequest(
        byte[] peerId,
        long remaining,
        long downloaded,
        long uploaded,
        Sha1Hash infoHash,
        TrackerEvent @event = TrackerEvent.None,
        int numWant = 200)
    {
        PeerId = peerId;
        Remaining = remaining;
        Downloaded = downloaded;
        Uploaded = uploaded;
        InfoHash = infoHash;
        Event = @event;
        NumWant = numWant;
    }

    public byte[] PeerId { get; }
    public long Remaining { get; }
    public long Downloaded { get; }
    public long Uploaded { get; }
    public Sha1Hash InfoHash { get; }
    public TrackerEvent Event { get; }
    public int NumWant { get; }
}
