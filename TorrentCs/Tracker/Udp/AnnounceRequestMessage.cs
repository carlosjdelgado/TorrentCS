using System.Net;
using TorrentCs.Data;
using TorrentCs.Transport;

namespace TorrentCs.Tracker.Udp;

public class AnnounceRequestMessage : UdpTrackerRequestMessage
{
    public enum EventType
    {
        None = 0,
        Completed = 1,
        Started = 2,
        Stopped = 3,
    }

    public AnnounceRequestMessage()
    {
        Action = MessageAction.Announce;
    }

    public Sha1Hash InfoHash { get; set; } = Sha1Hash.Empty;
    public byte[] PeerId { get; set; } = new byte[20];
    public long Downloaded { get; set; }
    public long LeftToDownload { get; set; }
    public long Uploaded { get; set; }
    public EventType Event { get; set; } = EventType.None;
    public IPAddress IPAddress { get; set; } = IPAddress.Any;
    public int Key { get; set; }
    public int NumWant { get; set; } = -1;
    public ushort Port { get; set; }

    public override void WriteTo(BinaryWriter writer)
    {
        var be = new BigEndianBinaryWriter(writer.BaseStream);
        be.Write(ConnectionId);
        be.Write((int)Action);
        be.Write(TransactionId);
        writer.Write(InfoHash.Value);
        writer.Write(PeerId);
        be.Write(Downloaded);
        be.Write(LeftToDownload);
        be.Write(Uploaded);
        be.Write((int)Event);
        writer.Write(IPAddress.GetAddressBytes());
        be.Write(Key);
        be.Write(NumWant);
        be.Write(Port);
    }
}
