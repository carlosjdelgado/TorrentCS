using System.Net.Sockets;
using System.Text;
using TorrentCs.Application.BitTorrent.Messages;
using TorrentCs.Data;
using TorrentCs.Engine;
using TorrentCs.Transport;

namespace TorrentCs.Application.BitTorrent;

public class BitTorrentPeer : IPeer, IChokablePeer
{
    private readonly ITransportStream _stream;
    private readonly BigEndianBinaryWriter _writer;
    private readonly RateMeasurer _downloadRate = new();
    private IPeerMessageHandler? _messageHandler;

    public BitTorrentPeer(ITransportStream stream, Metainfo metainfo)
    {
        _stream = stream;
        _writer = new BigEndianBinaryWriter(stream.Stream);
        Available = new Bitfield(metainfo.Pieces.Count);
        Address = stream.DisplayAddress;
        Values = new Dictionary<string, object>();
    }

    public PeerId PeerId { get; private set; } = new(new byte[20]);
    public string Address { get; }
    public Bitfield Available { get; }
    public byte[] ReservedBytes { get; private set; } = new byte[8];
    public ProtocolExtension SupportedExtensions { get; private set; }
    public bool IsChokedByRemotePeer { get; set; } = true;
    public bool IsInterestedInRemotePeer { get; set; }
    public bool IsChokingRemotePeer { get; set; } = true;
    public bool IsInterestedInPeer { get; set; }
    public Dictionary<string, object> Values { get; }

    public void SetHandler(IPeerMessageHandler handler) => _messageHandler = handler;

    public void SendMessage(byte messageId, byte[] data)
    {
        try
        {
            lock (_writer)
            {
                _writer.Write(1 + data.Length); // length prefix
                _stream.Stream.WriteByte(messageId);
                _stream.Stream.Write(data, 0, data.Length);
                _stream.Stream.Flush();
            }
        }
        catch (Exception ex) when (ex is IOException or SocketException or ObjectDisposedException)
        {
            // The peer's connection is gone (reset/broken pipe/closed). Drop just this peer; the
            // receive loop reports the disconnect. Never let one dead peer abort the whole download.
            Disconnect();
        }
    }

    /// <summary>Records bytes received from this peer, for download-rate / tit-for-tat tracking.</summary>
    public void RecordDownloaded(int bytes) => _downloadRate.AddMeasure(bytes);

    public long DownloadRate() => _downloadRate.AverageRate();

    public void Choke()
    {
        if (IsChokingRemotePeer) return;
        IsChokingRemotePeer = true;
        SendMessage(ChokeMessage.MessageID, []);
    }

    public void Unchoke()
    {
        if (!IsChokingRemotePeer) return;
        IsChokingRemotePeer = false;
        SendMessage(UnchokeMessage.MessageID, []);
    }

    public void Disconnect() => _stream.Disconnect();

    /// <summary>The info-hash, peer id and reserved bytes read from an incoming handshake.</summary>
    public readonly record struct IncomingHandshake(Sha1Hash InfoHash, byte[] PeerId, byte[] ReservedBytes);

    /// <summary>
    /// Reads the handshake an initiating peer sends, without verifying the info-hash. Used to route
    /// an incoming connection to the matching torrent before a peer object is created.
    /// </summary>
    public static async Task<IncomingHandshake> ReadIncomingHandshakeAsync(
        Stream stream, CancellationToken ct = default)
    {
        int pstrLen = stream.ReadByte();
        if (pstrLen < 0) throw new EndOfStreamException();

        var pstr = new byte[pstrLen];
        await ReadExactlyAsync(stream, pstr, ct);
        if (Encoding.ASCII.GetString(pstr) != "BitTorrent protocol")
            throw new InvalidDataException("Unexpected protocol identifier in handshake.");

        var reserved = new byte[8];
        await ReadExactlyAsync(stream, reserved, ct);

        var infoHash = new byte[20];
        await ReadExactlyAsync(stream, infoHash, ct);

        var peerId = new byte[20];
        await ReadExactlyAsync(stream, peerId, ct);

        return new IncomingHandshake(new Sha1Hash(infoHash), peerId, reserved);
    }

    /// <summary>Records the remote peer's handshake details (used for already-read incoming connections).</summary>
    public void ApplyRemoteHandshake(byte[] reservedBytes, byte[] remotePeerId)
    {
        ReservedBytes = reservedBytes;
        SupportedExtensions = ProtocolExtensions.DetermineSupportedProtocolExtensions(reservedBytes);
        PeerId = new PeerId(remotePeerId);
    }

    /// <summary>Sends our handshake in reply to an incoming connection whose handshake was read already.</summary>
    public Task SendHandshakeResponseAsync(Metainfo metainfo, PeerId localPeerId, byte[] reservedBytes)
        => SendHandshakeAsync(_stream.Stream, metainfo.InfoHash, localPeerId, reservedBytes);

    public async Task PerformHandshakeAsync(
        Metainfo metainfo, PeerId localPeerId, byte[] reservedBytes, bool isInitiator)
    {
        var ns = _stream.Stream;

        if (isInitiator)
        {
            await SendHandshakeAsync(ns, metainfo.InfoHash, localPeerId, reservedBytes);
            await ReceiveHandshakeAsync(ns, metainfo.InfoHash);
        }
        else
        {
            await ReceiveHandshakeAsync(ns, metainfo.InfoHash);
            await SendHandshakeAsync(ns, metainfo.InfoHash, localPeerId, reservedBytes);
        }
    }

    public async Task ReceiveMessagesAsync(CancellationToken ct)
    {
        var ns = _stream.Stream;
        var lengthBuf = new byte[4];
        var reader = new BinaryReader(ns);

        while (!ct.IsCancellationRequested)
        {
            try
            {
                await ReadExactlyAsync(ns, lengthBuf, ct);
                int length = (lengthBuf[0] << 24) | (lengthBuf[1] << 16) |
                             (lengthBuf[2] << 8) | lengthBuf[3];

                if (length == 0) continue; // keep-alive

                byte id = (byte)ns.ReadByte();
                _messageHandler?.MessageReceived(id, length, reader, this);
            }
            catch (OperationCanceledException) { break; }
            catch { break; }
        }

        _messageHandler?.PeerDisconnected(this);
    }

    private static async Task SendHandshakeAsync(
        Stream stream, Sha1Hash infoHash, PeerId peerId, byte[] reservedBytes)
    {
        var pstr = Encoding.ASCII.GetBytes("BitTorrent protocol");
        stream.WriteByte((byte)pstr.Length);
        await stream.WriteAsync(pstr);
        await stream.WriteAsync(reservedBytes);
        await stream.WriteAsync(infoHash.Value);
        await stream.WriteAsync(peerId.Value);
        await stream.FlushAsync();
    }

    private async Task ReceiveHandshakeAsync(Stream stream, Sha1Hash expectedInfoHash)
    {
        int pstrLen = stream.ReadByte();
        if (pstrLen < 0) throw new EndOfStreamException();

        var pstr = new byte[pstrLen];
        await ReadExactlyAsync(stream, pstr);

        var reserved = new byte[8];
        await ReadExactlyAsync(stream, reserved);

        var infoHash = new byte[20];
        await ReadExactlyAsync(stream, infoHash);

        var peerId = new byte[20];
        await ReadExactlyAsync(stream, peerId);

        if (!infoHash.SequenceEqual(expectedInfoHash.Value))
            throw new InvalidDataException("Info hash mismatch in handshake.");

        ApplyRemoteHandshake(reserved, peerId);
    }

    private static async Task ReadExactlyAsync(Stream stream, byte[] buffer, CancellationToken ct = default)
    {
        int offset = 0;
        while (offset < buffer.Length)
        {
            int read = await stream.ReadAsync(buffer, offset, buffer.Length - offset, ct);
            if (read == 0) throw new EndOfStreamException();
            offset += read;
        }
    }
}
