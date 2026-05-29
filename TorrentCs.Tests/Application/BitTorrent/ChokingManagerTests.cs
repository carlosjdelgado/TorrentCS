using TorrentCs.Application.BitTorrent;

namespace TorrentCs.Tests.Application.BitTorrent;

public class ChokingManagerTests
{
    [Fact]
    public void SelectUnchoked_PrefersHighestDownloadRate()
    {
        var peers = new List<IChokablePeer>
        {
            new FakePeer(rate: 100, interested: true),
            new FakePeer(rate: 500, interested: true),
            new FakePeer(rate: 300, interested: true),
            new FakePeer(rate: 50, interested: true),
            new FakePeer(rate: 900, interested: true),
        };
        var manager = new ChokingManager(maxUnchoked: 2);

        var selected = manager.SelectUnchoked(peers, optimistic: false);

        Assert.Equal(2, selected.Count);
        Assert.Contains(peers[4], selected); // 900
        Assert.Contains(peers[1], selected); // 500
    }

    [Fact]
    public void SelectUnchoked_IgnoresUninterestedPeers()
    {
        var peers = new List<IChokablePeer>
        {
            new FakePeer(rate: 1000, interested: false),
            new FakePeer(rate: 10, interested: true),
        };
        var manager = new ChokingManager(maxUnchoked: 4);

        var selected = manager.SelectUnchoked(peers, optimistic: false);

        Assert.Single(selected);
        Assert.Contains(peers[1], selected);
    }

    [Fact]
    public void SelectUnchoked_NoInterestedPeers_ReturnsEmpty()
    {
        var peers = new List<IChokablePeer>
        {
            new FakePeer(rate: 100, interested: false),
        };
        var manager = new ChokingManager(maxUnchoked: 4);

        Assert.Empty(manager.SelectUnchoked(peers, optimistic: false));
    }

    [Fact]
    public void SelectUnchoked_Optimistic_ReservesOneSlotForRandomPeer()
    {
        // 4 interested peers, maxUnchoked=2, optimistic on:
        // 1 regular slot (highest rate) + 1 optimistic (random among the rest).
        var peers = new List<IChokablePeer>
        {
            new FakePeer(rate: 900, interested: true),
            new FakePeer(rate: 100, interested: true),
            new FakePeer(rate: 200, interested: true),
            new FakePeer(rate: 300, interested: true),
        };
        var manager = new ChokingManager(maxUnchoked: 2, random: new Random(1));

        var selected = manager.SelectUnchoked(peers, optimistic: true);

        Assert.Equal(2, selected.Count);
        Assert.Contains(peers[0], selected); // top rate always gets the regular slot
    }

    [Fact]
    public void Update_UnchokesSelectedAndChokesTheRest()
    {
        var keep = new FakePeer(rate: 900, interested: true) { IsChokingRemotePeer = true };
        var drop = new FakePeer(rate: 10, interested: true) { IsChokingRemotePeer = false };
        var peers = new List<IChokablePeer> { keep, drop };

        // maxUnchoked=1, no optimistic on first call? first Update has _updateCount=0 → optimistic.
        // Use maxUnchoked=1 so only the top peer is kept regardless.
        var manager = new ChokingManager(maxUnchoked: 1, random: new Random(0));
        manager.Update(peers);

        Assert.False(keep.IsChokingRemotePeer); // unchoked (top rate)
        Assert.True(drop.IsChokingRemotePeer);  // choked
    }

    [Fact]
    public void Update_DoesNotResendWhenStateUnchanged()
    {
        var peer = new FakePeer(rate: 100, interested: true) { IsChokingRemotePeer = false };
        var manager = new ChokingManager(maxUnchoked: 4);

        manager.Update([peer]);

        // Already unchoked and still selected → no redundant Unchoke() call.
        Assert.Equal(0, peer.UnchokeCalls);
        Assert.False(peer.IsChokingRemotePeer);
    }

    [Fact]
    public void FillFreeSlots_UnchokesInterestedPeerWhenSlotFree()
    {
        var peer = new FakePeer(rate: 0, interested: true) { IsChokingRemotePeer = true };
        var manager = new ChokingManager(maxUnchoked: 4);

        manager.FillFreeSlots([peer]);

        Assert.False(peer.IsChokingRemotePeer);
        Assert.Equal(1, peer.UnchokeCalls);
    }

    [Fact]
    public void FillFreeSlots_DoesNothingWhenSlotsFull()
    {
        var unchoked = new FakePeer(rate: 0, interested: true) { IsChokingRemotePeer = false };
        var waiting = new FakePeer(rate: 0, interested: true) { IsChokingRemotePeer = true };
        var manager = new ChokingManager(maxUnchoked: 1);

        manager.FillFreeSlots([unchoked, waiting]);

        Assert.True(waiting.IsChokingRemotePeer); // no free slot, stays choked
    }

    [Fact]
    public void FillFreeSlots_NeverChokes()
    {
        var unchoked = new FakePeer(rate: 0, interested: false) { IsChokingRemotePeer = false };
        var manager = new ChokingManager(maxUnchoked: 4);

        manager.FillFreeSlots([unchoked]);

        Assert.False(unchoked.IsChokingRemotePeer); // left as-is, FillFreeSlots only unchokes
        Assert.Equal(0, unchoked.ChokeCalls);
    }

    [Fact]
    public void Constructor_RejectsZeroSlots()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new ChokingManager(maxUnchoked: 0));
    }

    private sealed class FakePeer(long rate, bool interested) : IChokablePeer
    {
        public bool IsInterestedInRemotePeer { get; } = interested;
        public bool IsChokingRemotePeer { get; set; } = true;
        public int ChokeCalls { get; private set; }
        public int UnchokeCalls { get; private set; }

        public long DownloadRate() => rate;

        public void Choke()
        {
            ChokeCalls++;
            IsChokingRemotePeer = true;
        }

        public void Unchoke()
        {
            UnchokeCalls++;
            IsChokingRemotePeer = false;
        }
    }
}
