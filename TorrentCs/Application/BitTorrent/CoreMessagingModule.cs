using System.Net;
using TorrentCs.Application.BitTorrent.Messages;
using TorrentCs.Data;
using TorrentCs.Modularity;
using TorrentCs.Transport;

namespace TorrentCs.Application.BitTorrent;

public class CoreMessagingModule : IModule
{
    public void OnPrepareHandshake(IPrepareHandshakeContext context) { }

    public void OnPeerConnected(IPeerContext context)
    {
        for (byte id = ChokeMessage.MessageID; id <= CancelMessage.MessageID; id++)
            context.RegisterMessageHandler(id);

        var completed = context.DataHandler.CompletedPieces.ToList();
        if (completed.Count <= 0) return;

        var bitfield = new Bitfield(context.Metainfo.Pieces.Count);
        foreach (var piece in completed)
            bitfield.SetPieceAvailable(piece.Index, true);
        context.SendMessage(BitfieldMessage.MessageID, bitfield.ToBytes());
    }

    public void OnMessageReceived(IMessageReceivedContext context)
    {
        var peer = (BitTorrentPeer)context.Peer;
        var msg = MessageHandler.ReadMessage(
            context.Metainfo, context.Reader, context.MessageLength, (byte)context.MessageId);
        if (msg is null) return;

        switch (msg)
        {
            case ChokeMessage:
                peer.IsChokedByRemotePeer = true;
                break;
            case UnchokeMessage:
                peer.IsChokedByRemotePeer = false;
                break;
            case InterestedMessage:
                // Mark interest only; the ChokingManager decides if/when to unchoke this peer.
                peer.IsInterestedInRemotePeer = true;
                break;
            case NotInterestedMessage:
                peer.IsInterestedInRemotePeer = false;
                break;
            case HaveMessage have when have.Piece is not null:
                peer.Available.SetPieceAvailable(have.Piece.Index, true);
                UpdateInterest(context, peer);
                break;
            case BitfieldMessage bf when bf.Bitfield is not null:
                for (int i = 0; i < bf.Bitfield.PieceCount; i++)
                    peer.Available.SetPieceAvailable(i, bf.Bitfield.IsPieceAvailable(i));
                UpdateInterest(context, peer);
                break;
            case RequestMessage req when req.Block is not null && !peer.IsChokingRemotePeer:
                SendBlockData(context, req.Block);
                break;
            case PieceMessage piece when piece.Block is not null:
                long offset = (long)piece.Block.PieceIndex * context.Metainfo.PieceSize + piece.Block.Offset;
                context.DataHandler.WriteBlockData(offset, piece.Block.Data);
                context.BlockRequests.BlockReceived(
                    new Block(piece.Block.PieceIndex, piece.Block.Offset, piece.Block.Data));
                peer.RecordDownloaded(piece.Block.Data.Length); // for tit-for-tat rate tracking
                break;
            case CancelMessage cancel when cancel.Block is not null:
                context.BlockRequests.ClearBlocksForPiece(cancel.Block.PieceIndex);
                break;
        }
    }

    /// <summary>
    /// Sends an Interested message to the peer the first time it advertises a piece we still need.
    /// A remote peer only unchokes us (allowing us to request blocks) after we express interest.
    /// </summary>
    private static void UpdateInterest(IPeerContext context, BitTorrentPeer peer)
    {
        if (peer.IsInterestedInPeer) return;

        var completed = context.DataHandler.CompletedPieces
            .Select(p => p.Index)
            .ToHashSet();

        bool needsSomething = false;
        for (int i = 0; i < context.Metainfo.Pieces.Count; i++)
        {
            if (peer.Available.IsPieceAvailable(i) && !completed.Contains(i))
            {
                needsSomething = true;
                break;
            }
        }

        if (needsSomething)
        {
            peer.IsInterestedInPeer = true;
            context.SendMessage(InterestedMessage.MessageID, []);
        }
    }

    private static void SendBlockData(IPeerContext context, BlockRequest request)
    {
        long offset = (long)request.PieceIndex * context.Metainfo.PieceSize + request.Offset;
        if (!context.DataHandler.TryReadBlockData(offset, request.Length, out var data))
            return;

        // piece message body: pieceIndex(4BE) + blockOffset(4BE) + data(N)
        using var ms = new MemoryStream(8 + data.Length);
        var be = new BigEndianBinaryWriter(ms);
        be.Write(request.PieceIndex);
        be.Write(request.Offset);
        ms.Write(data);
        context.SendMessage(PieceMessage.MessageID, ms.ToArray());
    }
}
