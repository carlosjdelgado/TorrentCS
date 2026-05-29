using System.Net;
using System.Net.Sockets;
using System.Text;
using BencodeNET.Objects;
using BencodeNET.Parsing;
using TorrentCs.Extensions.ExtensionProtocol;

namespace TorrentCs.Extensions.PeerExchange;

/// <summary>
/// A Peer Exchange (BEP 11, "ut_pex") message: the compact lists of peers added to and dropped from
/// the sender's swarm since its previous message.
/// </summary>
internal sealed class PeerExchangeMessage : IExtensionProtocolMessage
{
    public const string Type = "ut_pex";

    public string MessageType => Type;

    public IList<IPEndPoint> Added { get; set; } = [];

    public IList<IPEndPoint> Dropped { get; set; } = [];

    public byte[] Serialize()
    {
        var dict = new BDictionary
        {
            ["added"] = EncodeCompact(Added),
            ["dropped"] = EncodeCompact(Dropped),
        };

        using var ms = new MemoryStream();
        dict.EncodeTo(ms);
        return ms.ToArray();
    }

    public void Deserialize(byte[] data)
    {
        var dict = new BencodeParser().Parse<BDictionary>(new MemoryStream(data));
        if (dict.TryGetValue("added", out var added) && added is BString addedPeers)
            Added = ParseCompact(addedPeers.Value.ToArray()).ToList();
        if (dict.TryGetValue("dropped", out var dropped) && dropped is BString droppedPeers)
            Dropped = ParseCompact(droppedPeers.Value.ToArray()).ToList();
    }

    private static BString EncodeCompact(IEnumerable<IPEndPoint> endpoints)
    {
        var bytes = new List<byte>();
        foreach (var endpoint in endpoints)
        {
            if (endpoint.AddressFamily != AddressFamily.InterNetwork) continue; // IPv4 only (BEP 11)
            bytes.AddRange(endpoint.Address.GetAddressBytes());
            bytes.Add((byte)(endpoint.Port >> 8));
            bytes.Add((byte)endpoint.Port);
        }
        return new BString(bytes.ToArray(), Encoding.Latin1);
    }

    private static IEnumerable<IPEndPoint> ParseCompact(byte[] bytes)
    {
        for (int i = 0; i + 5 < bytes.Length; i += 6)
        {
            var address = new IPAddress(bytes[i..(i + 4)]);
            int port = (bytes[i + 4] << 8) | bytes[i + 5];
            yield return new IPEndPoint(address, port);
        }
    }
}
