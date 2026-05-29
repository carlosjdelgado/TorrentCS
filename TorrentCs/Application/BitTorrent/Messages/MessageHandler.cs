using TorrentCs.Data;

namespace TorrentCs.Application.BitTorrent.Messages;

public static class MessageHandler
{
    private static readonly Dictionary<byte, Func<Metainfo, CommonPeerMessage>> Registry = new()
    {
        [ChokeMessage.MessageID] = _ => new ChokeMessage(),
        [UnchokeMessage.MessageID] = _ => new UnchokeMessage(),
        [InterestedMessage.MessageID] = _ => new InterestedMessage(),
        [NotInterestedMessage.MessageID] = _ => new NotInterestedMessage(),
        [HaveMessage.MessageID] = _ => new HaveMessage(),
        [BitfieldMessage.MessageID] = m => new BitfieldMessage(m.Pieces.Count),
        [RequestMessage.MessageID] = _ => new RequestMessage(),
        [PieceMessage.MessageID] = _ => new PieceMessage(),
        [CancelMessage.MessageID] = _ => new CancelMessage(),
    };

    public static CommonPeerMessage? ReadMessage(Metainfo metainfo, BinaryReader reader, int length, byte id)
    {
        if (!Registry.TryGetValue(id, out var factory))
            return null;
        var msg = factory(metainfo);
        msg.Receive(reader, length - 1); // -1 for the ID byte already read
        return msg;
    }
}
