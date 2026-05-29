using System.Text;
using TorrentCs.Data;
using TorrentCs.Transport;

namespace TorrentCs.Application.BitTorrent;

public class BitTorrentPeer : IPeer
{
    private readonly ITransportStream _stream;
    private readonly BigEndianBinaryWriter _writer;
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
        lock (_writer)
        {
            _writer.Write(1 + data.Length); // length prefix
            _stream.Stream.WriteByte(messageId);
            _stream.Stream.Write(data, 0, data.Length);
            _stream.Stream.Flush();
        }
    }

    public void Disconnect() => _stream.Disconnect();

    public async Task PerformHandshakeAsync(
        Metainfo metainfo, PeerId localPeerId, bool isInitiator)
    {
        var ns = _stream.Stream;

        if (isInitiator)
        {
            await SendHandshakeAsync(ns, metainfo.InfoHash, localPeerId);
            await ReceiveHandshakeAsync(ns, metainfo.InfoHash);
        }
        else
        {
            await ReceiveHandshakeAsync(ns, metainfo.InfoHash);
            await SendHandshakeAsync(ns, metainfo.InfoHash, localPeerId);
        }
    }

    public async Task ReceiveMessagesAsync(Metainfo metainfo, CancellationToken ct)
    {
        var ns = _stream.Stream;
        var lengthBuf = new byte[4];
        var be = new BigEndianBinaryReader(ns);
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

    private static async Task SendHandshakeAsync(Stream stream, Sha1Hash infoHash, PeerId peerId)
    {
        var pstr = Encoding.ASCII.GetBytes("BitTorrent protocol");
        stream.WriteByte((byte)pstr.Length);
        await stream.WriteAsync(pstr);
        await stream.WriteAsync(new byte[8]); // reserved
        await stream.WriteAsync(infoHash.Value);
        await stream.WriteAsync(peerId.Value);
        await stream.FlushAsync();
    }

    private static async Task ReceiveHandshakeAsync(Stream stream, Sha1Hash expectedInfoHash)
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
