namespace TorrentCs.Application.BitTorrent.Messages;

public class NotInterestedMessage : CommonPeerMessage
{
    public const byte MessageID = 3;
    public override byte ID => MessageID;
}
