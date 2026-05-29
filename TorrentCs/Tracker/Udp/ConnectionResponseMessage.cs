using TorrentCs.Transport;

namespace TorrentCs.Tracker.Udp;

public class ConnectionResponseMessage : UdpTrackerResponseMessage
{
    public ConnectionResponseMessage()
    {
        Action = MessageAction.Connect;
    }

    public long ConnectionId { get; private set; }

    public override void ReadFrom(BinaryReader reader)
    {
        var be = new BigEndianBinaryReader(reader.BaseStream);
        var action = (MessageAction)be.ReadInt32();
        if (action != Action)
            throw new InvalidDataException($"Expected action {Action}, got {action}.");
        TransactionId = be.ReadInt32();
        ConnectionId = be.ReadInt64();
    }
}
