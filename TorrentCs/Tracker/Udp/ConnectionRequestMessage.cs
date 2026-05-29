using TorrentCs.Transport;

namespace TorrentCs.Tracker.Udp;

public class ConnectionRequestMessage : UdpTrackerRequestMessage
{
    public ConnectionRequestMessage()
    {
        Action = MessageAction.Connect;
    }

    public override void WriteTo(BinaryWriter writer)
    {
        var be = new BigEndianBinaryWriter(writer.BaseStream);
        be.Write(ConnectionId);
        be.Write((int)Action);
        be.Write(TransactionId);
    }
}
