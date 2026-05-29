using TorrentCs.Transport;

namespace TorrentCs.Tests.Transport;

public class NullPortForwardingTests
{
    [Fact]
    public void MapPort_DoesNotThrow()
    {
        var pf = new NullPortForwarding();
        pf.MapPort(6881);
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var pf = new NullPortForwarding();
        pf.MapPort(6881);
        pf.Dispose();
    }
}
