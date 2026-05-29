using BencodeNET.Objects;
using BencodeNET.Parsing;
using TorrentCs.Extensions.ExtensionProtocol;

namespace TorrentCs.Extensions.SendMetadata;

/// <summary>
/// A metadata-exchange message (BEP 9, "ut_metadata"). The payload is a bencoded dictionary
/// (<c>msg_type</c>, <c>piece</c>, and for data messages <c>total_size</c>) followed, for data
/// messages, by the raw metadata piece appended after the dictionary.
/// </summary>
internal sealed class MetadataMessage : IExtensionProtocolMessage
{
    public const string Type = "ut_metadata";

    /// <summary>The size of each metadata piece, fixed by BEP 9.</summary>
    public const int PieceSize = 16 * 1024;

    public enum MessageType
    {
        Request = 0,
        Data = 1,
        Reject = 2,
    }

    string IExtensionProtocolMessage.MessageType => Type;

    public MessageType RequestType { get; set; }

    public int PieceIndex { get; set; }

    public int TotalSize { get; set; }

    public byte[] PieceData { get; set; } = [];

    public byte[] Serialize()
    {
        var dict = new BDictionary
        {
            ["msg_type"] = new BNumber((int)RequestType),
            ["piece"] = new BNumber(PieceIndex),
        };
        if (RequestType == MessageType.Data)
            dict["total_size"] = new BNumber(TotalSize);

        using var ms = new MemoryStream();
        dict.EncodeTo(ms);
        if (RequestType == MessageType.Data)
            ms.Write(PieceData, 0, PieceData.Length);
        return ms.ToArray();
    }

    public void Deserialize(byte[] data)
    {
        using var ms = new MemoryStream(data);
        var dict = new BDictionaryParser(new BencodeParser()).Parse(ms);

        RequestType = (MessageType)((BNumber)dict["msg_type"]).Value;
        PieceIndex = (int)((BNumber)dict["piece"]).Value;

        if (RequestType == MessageType.Data)
        {
            TotalSize = (int)((BNumber)dict["total_size"]).Value;
            PieceData = data[(int)ms.Position..]; // the raw piece follows the bencoded dictionary
        }
    }
}
