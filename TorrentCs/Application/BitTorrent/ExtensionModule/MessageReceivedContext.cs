using TorrentCs.Modularity;

namespace TorrentCs.Application.BitTorrent.ExtensionModule;

public class MessageReceivedContext : PeerContext, IMessageReceivedContext
{
    public MessageReceivedContext(
        PeerContext peerContext,
        int messageId,
        int messageLength,
        BinaryReader reader)
        : base(
            (BitTorrentPeer)peerContext.Peer,
            ((BitTorrentPeer)peerContext.Peer).Values,
            peerContext,
            _ => { })
    {
        MessageId = messageId;
        MessageLength = messageLength;
        Reader = reader;
    }

    public int MessageId { get; }
    public int MessageLength { get; }
    public BinaryReader Reader { get; }
}
