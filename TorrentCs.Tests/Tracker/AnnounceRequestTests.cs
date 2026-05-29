using TorrentCs.Data;
using TorrentCs.Tracker;

namespace TorrentCs.Tests.Tracker;

public class AnnounceRequestTests
{
    [Fact]
    public void Properties_AreSetCorrectly()
    {
        var peerId = new byte[20];
        var hash = new Sha1Hash(new byte[20]);
        var req = new AnnounceRequest(peerId, remaining: 100, downloaded: 200, uploaded: 50, hash);

        Assert.Same(peerId, req.PeerId);
        Assert.Equal(100, req.Remaining);
        Assert.Equal(200, req.Downloaded);
        Assert.Equal(50, req.Uploaded);
        Assert.Same(hash, req.InfoHash);
    }
}
