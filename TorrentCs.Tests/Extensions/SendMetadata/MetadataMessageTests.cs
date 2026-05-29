using TorrentCs.Extensions.SendMetadata;

namespace TorrentCs.Tests.Extensions.SendMetadata;

public class MetadataMessageTests
{
    [Fact]
    public void MessageType_IsUtMetadata()
    {
        Assert.Equal("ut_metadata", ((TorrentCs.Extensions.ExtensionProtocol.IExtensionProtocolMessage)
            new MetadataMessage()).MessageType);
    }

    [Fact]
    public void Serialize_Deserialize_RoundTripsRequest()
    {
        var original = new MetadataMessage
        {
            RequestType = MetadataMessage.MessageType.Request,
            PieceIndex = 3,
        };

        var roundTripped = new MetadataMessage();
        roundTripped.Deserialize(original.Serialize());

        Assert.Equal(MetadataMessage.MessageType.Request, roundTripped.RequestType);
        Assert.Equal(3, roundTripped.PieceIndex);
    }

    [Fact]
    public void Serialize_Deserialize_RoundTripsDataWithPieceAppended()
    {
        var piece = new byte[100];
        new Random(7).NextBytes(piece);
        var original = new MetadataMessage
        {
            RequestType = MetadataMessage.MessageType.Data,
            PieceIndex = 1,
            TotalSize = 1000,
            PieceData = piece,
        };

        var roundTripped = new MetadataMessage();
        roundTripped.Deserialize(original.Serialize());

        Assert.Equal(MetadataMessage.MessageType.Data, roundTripped.RequestType);
        Assert.Equal(1, roundTripped.PieceIndex);
        Assert.Equal(1000, roundTripped.TotalSize);
        Assert.Equal(piece, roundTripped.PieceData);
    }
}
