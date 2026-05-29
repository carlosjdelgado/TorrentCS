namespace TorrentCs.Application.BitTorrent.Messages;

public class InterestedMessage : CommonPeerMessage
{
    public const byte MessageID = 2;
    public override byte ID => MessageID;
}
