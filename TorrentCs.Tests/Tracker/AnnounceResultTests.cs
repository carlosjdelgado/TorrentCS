using TorrentCs.Tracker;
using TorrentCs.Transport;

namespace TorrentCs.Tests.Tracker;

public class AnnounceResultTests
{
    [Fact]
    public void Peers_ContainsAllProvidedStreams()
    {
        var streams = new[] { new FakeTransportStream(), new FakeTransportStream() };
        var result = new AnnounceResult(streams);
        Assert.Equal(2, result.Peers.Count);
    }

    [Fact]
    public void Peers_EmptyEnumerable_IsEmpty()
    {
        var result = new AnnounceResult([]);
        Assert.Empty(result.Peers);
    }

    private sealed class FakeTransportStream : ITransportStream
    {
        public bool IsConnected => false;
        public string DisplayAddress => "fake";
        public object Address => "fake";
        public Stream Stream => Stream.Null;
        public Task Connect() => Task.CompletedTask;
        public void Disconnect() { }
    }
}
