using BencodeNET.Objects;
using BencodeNET.Parsing;

namespace TorrentCs.Extensions.ExtensionProtocol;

/// <summary>
/// The BEP 10 extended handshake (extended message id 0): the <c>m</c> dictionary mapping supported
/// message names to ids, the client <c>v</c> string, and our TCP listen port <c>p</c>.
/// </summary>
internal sealed class ExtensionProtocolHandshake
{
    public Dictionary<string, byte> MessageIds { get; set; } = [];

    public string? Client { get; set; }

    public int? ListenPort { get; set; }

    public BDictionary Serialize()
    {
        var m = new BDictionary();
        foreach (var (name, id) in MessageIds)
            m[name] = new BNumber(id);

        var dict = new BDictionary { ["m"] = m };
        if (Client is not null)
            dict["v"] = new BString(Client);
        if (ListenPort is int port)
            dict["p"] = new BNumber(port);
        return dict;
    }

    public void Deserialize(byte[] data)
    {
        var dict = new BencodeParser().Parse<BDictionary>(new MemoryStream(data));

        if (dict.TryGetValue("m", out var m) && m is BDictionary messageIds)
        {
            foreach (var (key, value) in messageIds)
            {
                if (value is BNumber number)
                    MessageIds[key.ToString()] = (byte)number.Value;
            }
        }

        if (dict.TryGetValue("v", out var v) && v is BString client)
            Client = client.ToString();

        if (dict.TryGetValue("p", out var p) && p is BNumber port && port.Value is > 0 and <= 65535)
            ListenPort = (int)port.Value;
    }
}
