namespace TorrentCs.Tracker.Udp;

public abstract class UdpTrackerResponseMessage
{
    protected MessageAction Action { get; set; }
    public int TransactionId { get; protected set; }

    public abstract void ReadFrom(BinaryReader reader);
}
