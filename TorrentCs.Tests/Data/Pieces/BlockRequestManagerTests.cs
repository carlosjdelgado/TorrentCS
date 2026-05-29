using TorrentCs.Data;
using TorrentCs.Data.Pieces;

namespace TorrentCs.Tests.Data.Pieces;

public class BlockRequestManagerTests
{
    [Fact]
    public void BlockRequested_AddsToRequestedBlocks()
    {
        var manager = new BlockRequestManager();
        var block = new Block(0, 0, new byte[16]);
        manager.BlockRequested(block);
        Assert.Contains(block, manager.RequestedBlocks);
    }

    [Fact]
    public void BlockReceived_MovesToDownloadedBlocks()
    {
        var manager = new BlockRequestManager();
        var block = new Block(0, 0, new byte[16]);
        manager.BlockRequested(block);
        manager.BlockReceived(block);

        Assert.DoesNotContain(block, manager.RequestedBlocks);
        Assert.Contains(block, manager.DownloadedBlocks);
    }

    [Fact]
    public void BlockReceived_WithoutRequest_StillAddsToDownloaded()
    {
        var manager = new BlockRequestManager();
        var block = new Block(1, 16384, new byte[16]);
        manager.BlockReceived(block);
        Assert.Contains(block, manager.DownloadedBlocks);
    }

    [Fact]
    public void ClearBlocksForPiece_RemovesFromBothCollections()
    {
        var manager = new BlockRequestManager();
        var req = new Block(2, 0, new byte[8]);
        var dl = new Block(2, 8, new byte[8]);
        var other = new Block(3, 0, new byte[8]);

        manager.BlockRequested(req);
        manager.BlockReceived(dl);
        manager.BlockRequested(other);

        manager.ClearBlocksForPiece(2);

        Assert.DoesNotContain(req, manager.RequestedBlocks);
        Assert.DoesNotContain(dl, manager.DownloadedBlocks);
        Assert.Contains(other, manager.RequestedBlocks);
    }

    [Fact]
    public void DuplicateBlock_IsStoredOnce()
    {
        var manager = new BlockRequestManager();
        var block = new Block(0, 0, new byte[8]);
        manager.BlockRequested(block);
        manager.BlockRequested(block);
        Assert.Single(manager.RequestedBlocks);
    }

    [Fact]
    public void ExpireStaleRequests_RemovesOldRequests()
    {
        var manager = new BlockRequestManager();
        var block = new Block(0, 0, new byte[16]);
        manager.BlockRequested(block);

        Thread.Sleep(20);
        manager.ExpireStaleRequests(TimeSpan.FromMilliseconds(10));

        Assert.Empty(manager.RequestedBlocks);
    }

    [Fact]
    public void ExpireStaleRequests_KeepsRecentRequests()
    {
        var manager = new BlockRequestManager();
        var block = new Block(0, 0, new byte[16]);
        manager.BlockRequested(block);

        manager.ExpireStaleRequests(TimeSpan.FromSeconds(60));

        Assert.Contains(block, manager.RequestedBlocks);
    }

    [Fact]
    public void ExpireStaleRequests_DoesNotTouchDownloadedBlocks()
    {
        var manager = new BlockRequestManager();
        var block = new Block(0, 0, new byte[16]);
        manager.BlockRequested(block);
        manager.BlockReceived(block);

        Thread.Sleep(20);
        manager.ExpireStaleRequests(TimeSpan.FromMilliseconds(10));

        Assert.Contains(block, manager.DownloadedBlocks);
    }
}
