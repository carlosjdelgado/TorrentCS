using System.Net;
using TorrentCs.Serialization;
using TorrentCs.Transport;

namespace TorrentCs.Tracker.Udp;

public class AnnounceResponseMessage : UdpTrackerResponseMessage
{
    public AnnounceResponseMessage()
    {
        Action = MessageAction.Announce;
    }

    public int Interval { get; private set; }
    public int Leechers { get; private set; }
    public int Seeders { get; private set; }
    public IList<AnnounceResultPeer> Peers { get; } = new List<AnnounceResultPeer>();

    public override void ReadFrom(BinaryReader reader)
    {
        var be = new BigEndianBinaryReader(reader.BaseStream);
        var action = (MessageAction)be.ReadInt32();
        if (action != Action)
            throw new InvalidDataException($"Expected action {Action}, got {action}.");
        TransactionId = be.ReadInt32();
        Interval = be.ReadInt32();
        Leechers = be.ReadInt32();
        Seeders = be.ReadInt32();

        while (reader.BaseStream.Position < reader.BaseStream.Length)
        {
            var endpoint = reader.ReadIpV4EndPoint();
            Peers.Add(new AnnounceResultPeer(endpoint.Address, endpoint.Port));
        }
    }
}
