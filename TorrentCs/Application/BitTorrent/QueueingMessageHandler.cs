using TorrentCs.Engine;

namespace TorrentCs.Application.BitTorrent;

public class QueueingMessageHandler : IPeerMessageHandler
{
    private readonly IMainLoop _mainLoop;
    private readonly IPeerMessageHandler _inner;

    public QueueingMessageHandler(IMainLoop mainLoop, IPeerMessageHandler inner)
    {
        _mainLoop = mainLoop;
        _inner = inner;
    }

    public void MessageReceived(byte messageId, int length, BinaryReader reader, BitTorrentPeer peer)
        => _mainLoop.AddTask(() => _inner.MessageReceived(messageId, length, reader, peer));

    public void PeerDisconnected(BitTorrentPeer peer)
        => _mainLoop.AddTask(() => _inner.PeerDisconnected(peer));
}
