namespace TorrentCs.Application.BitTorrent.Messages;

public class UnchokeMessage : CommonPeerMessage
{
    public const byte MessageID = 1;
    public override byte ID => MessageID;
}
