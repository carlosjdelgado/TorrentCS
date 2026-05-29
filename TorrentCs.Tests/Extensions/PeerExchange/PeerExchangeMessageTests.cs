using System.Net;
using TorrentCs.Extensions.PeerExchange;

namespace TorrentCs.Tests.Extensions.PeerExchange;

public class PeerExchangeMessageTests
{
    [Fact]
    public void MessageType_IsUtPex()
    {
        Assert.Equal("ut_pex", new PeerExchangeMessage().MessageType);
    }

    [Fact]
    public void Serialize_Deserialize_RoundTripsAddedAndDropped()
    {
        var original = new PeerExchangeMessage
        {
            Added =
            [
                new IPEndPoint(IPAddress.Parse("1.2.3.4"), 6881),
                new IPEndPoint(IPAddress.Parse("10.0.0.5"), 51413),
            ],
            Dropped = [new IPEndPoint(IPAddress.Parse("9.9.9.9"), 1234)],
        };

        var roundTripped = new PeerExchangeMessage();
        roundTripped.Deserialize(original.Serialize());

        Assert.Equal(original.Added, roundTripped.Added);
        Assert.Equal(original.Dropped, roundTripped.Dropped);
    }

    [Fact]
    public void Deserialize_EmptyDictionary_LeavesListsEmpty()
    {
        var message = new PeerExchangeMessage();
        message.Deserialize([(byte)'d', (byte)'e']); // bencoded empty dictionary

        Assert.Empty(message.Added);
        Assert.Empty(message.Dropped);
    }
}
