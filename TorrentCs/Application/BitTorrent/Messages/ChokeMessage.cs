namespace TorrentCs.Application.BitTorrent.Messages;

public class ChokeMessage : CommonPeerMessage
{
    public const byte MessageID = 0;
    public override byte ID => MessageID;
}
