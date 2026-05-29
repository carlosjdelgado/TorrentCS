namespace TorrentCs.Tracker.Udp;

public abstract class UdpTrackerRequestMessage
{
    public long ConnectionId { get; set; }
    public int TransactionId { get; set; }
    protected MessageAction Action { get; set; }

    public abstract void WriteTo(BinaryWriter writer);
}
