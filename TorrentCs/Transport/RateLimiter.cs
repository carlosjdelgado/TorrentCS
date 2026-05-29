namespace TorrentCs.Transport;

public class RateLimiter
{
    private readonly object _uploadLock = new();
    private readonly object _downloadLock = new();
    private long _uploadedBytes;
    private long _downloadedBytes;
    private long _uploadWindowStart;
    private long _downloadWindowStart;

    public RateLimiter()
    {
        MaxUploadRate = -1;
        MaxDownloadRate = -1;
    }

    public RateLimiter(long maxUploadRate, long maxDownloadRate)
    {
        MaxUploadRate = maxUploadRate;
        MaxDownloadRate = maxDownloadRate;
        _uploadWindowStart = CurrentMilliseconds;
        _downloadWindowStart = CurrentMilliseconds;
    }

    public long MaxUploadRate { get; set; }
    public long MaxDownloadRate { get; set; }
    public bool IsUploadLimited => MaxUploadRate >= 0;
    public bool IsDownloadLimited => MaxDownloadRate >= 0;

    protected virtual long CurrentMilliseconds => Environment.TickCount64;

    public int TimeUntilCanSend(long length)
    {
        if (!IsUploadLimited) return 0;

        lock (_uploadLock)
        {
            long now = CurrentMilliseconds;
            long elapsed = now - _uploadWindowStart;

            if (elapsed >= 1000)
            {
                _uploadedBytes = length;
                _uploadWindowStart = now;
                return 0;
            }

            _uploadedBytes += length;
            long allowed = MaxUploadRate * (elapsed + 1) / 1000;
            if (_uploadedBytes <= allowed) return 0;

            return (int)((_uploadedBytes - allowed) * 1000 / MaxUploadRate);
        }
    }

    public int TimeUntilCanReceive(long length)
    {
        if (!IsDownloadLimited) return 0;

        lock (_downloadLock)
        {
            long now = CurrentMilliseconds;
            long elapsed = now - _downloadWindowStart;

            if (elapsed >= 1000)
            {
                _downloadedBytes = length;
                _downloadWindowStart = now;
                return 0;
            }

            _downloadedBytes += length;
            long allowed = MaxDownloadRate * (elapsed + 1) / 1000;
            if (_downloadedBytes <= allowed) return 0;

            return (int)((_downloadedBytes - allowed) * 1000 / MaxDownloadRate);
        }
    }

    public void ResetDownload()
    {
        lock (_downloadLock)
        {
            _downloadedBytes = 0;
            _downloadWindowStart = CurrentMilliseconds;
        }
    }
}
