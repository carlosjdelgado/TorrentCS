using TorrentCs.Transport;

namespace TorrentCs.Tests.Transport;

public class RateLimiterTests
{
    [Fact]
    public void Unlimited_TimeUntilCanSend_ReturnsZero()
    {
        var limiter = new RateLimiter();
        Assert.Equal(0, limiter.TimeUntilCanSend(1_000_000));
    }

    [Fact]
    public void Unlimited_TimeUntilCanReceive_ReturnsZero()
    {
        var limiter = new RateLimiter();
        Assert.Equal(0, limiter.TimeUntilCanReceive(1_000_000));
    }

    [Fact]
    public void Unlimited_IsUploadLimited_ReturnsFalse()
    {
        var limiter = new RateLimiter();
        Assert.False(limiter.IsUploadLimited);
    }

    [Fact]
    public void Unlimited_IsDownloadLimited_ReturnsFalse()
    {
        var limiter = new RateLimiter();
        Assert.False(limiter.IsDownloadLimited);
    }

    [Fact]
    public void Limited_IsUploadLimited_ReturnsTrue()
    {
        var limiter = new RateLimiter(1024, 1024);
        Assert.True(limiter.IsUploadLimited);
    }

    [Fact]
    public void Limited_IsDownloadLimited_ReturnsTrue()
    {
        var limiter = new RateLimiter(1024, 1024);
        Assert.True(limiter.IsDownloadLimited);
    }

    [Fact]
    public void Limited_SendingWithinBudget_ReturnsZero()
    {
        var limiter = new FrozenRateLimiter(1024, 1024, frozenMs: 0);
        // At t=0, allowed = 1024 * 1 / 1000 = 1 byte; but after the window resets it allows again
        // Since elapsed=0 means we're at start of window, send a small amount
        Assert.Equal(0, limiter.TimeUntilCanSend(1));
    }

    [Fact]
    public void Limited_ExceedingBudget_ReturnsPositiveDelay()
    {
        // 100 bytes/s limit, frozen at t=500ms in window → allowed=50 bytes
        var limiter = new FrozenRateLimiter(maxUpload: 100, maxDownload: 100, frozenMs: 500);
        limiter.TimeUntilCanSend(1); // consume 1 byte to initialize window
        var delay = limiter.TimeUntilCanSend(200); // try to send 200 more bytes (201 total > 50 allowed)
        Assert.True(delay > 0);
    }

    [Fact]
    public void Limited_WindowReset_AllowsNewBurst()
    {
        // After 1000ms, window resets and we can send freely again
        var limiter = new FrozenRateLimiter(maxUpload: 100, maxDownload: 100, frozenMs: 0);
        limiter.TimeUntilCanSend(500); // exhaust first window
        // Advance clock past 1 second
        limiter.FrozenMs = 1001;
        Assert.Equal(0, limiter.TimeUntilCanSend(1));
    }

    [Fact]
    public void ResetDownload_ResetsCounter()
    {
        // 100 bytes/s; freeze at 500ms into first window
        var limiter = new FrozenRateLimiter(maxUpload: 1024, maxDownload: 100, frozenMs: 500);
        limiter.TimeUntilCanReceive(200); // exceed the ~50-byte budget for 500ms

        Assert.True(limiter.TimeUntilCanReceive(1) > 0); // confirm rate-limited

        // Advance past 1s and reset
        limiter.FrozenMs = 1001;
        limiter.ResetDownload(); // _downloadWindowStart = 1001, _downloadedBytes = 0

        // Advance 500ms into the fresh window → allowed = 100 * 501/1000 = 50 bytes
        limiter.FrozenMs = 1501;
        Assert.Equal(0, limiter.TimeUntilCanReceive(1));
    }

    private sealed class FrozenRateLimiter(long maxUpload, long maxDownload, long frozenMs)
        : RateLimiter(maxUpload, maxDownload)
    {
        public long FrozenMs { get; set; } = frozenMs;
        protected override long CurrentMilliseconds => FrozenMs;
    }
}
