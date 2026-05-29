namespace TorrentCs.Transport;

public class RateLimitedStream : Stream
{
    public RateLimitedStream(Stream baseStream, RateLimiter limiter)
    {
        BaseStream = baseStream;
        Limiter = limiter;
    }

    public Stream BaseStream { get; }
    public RateLimiter Limiter { get; }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int delay = Limiter.TimeUntilCanReceive(count);
        if (delay > 0) Thread.Sleep(delay);
        return BaseStream.Read(buffer, offset, count);
    }

    public override void Write(byte[] buffer, int offset, int count)
    {
        int delay = Limiter.TimeUntilCanSend(count);
        if (delay > 0) Thread.Sleep(delay);
        BaseStream.Write(buffer, offset, count);
    }

    public override bool CanRead => BaseStream.CanRead;
    public override bool CanSeek => BaseStream.CanSeek;
    public override bool CanWrite => BaseStream.CanWrite;
    public override long Length => BaseStream.Length;
    public override long Position
    {
        get => BaseStream.Position;
        set => BaseStream.Position = value;
    }

    public override void Flush() => BaseStream.Flush();
    public override long Seek(long offset, SeekOrigin origin) => BaseStream.Seek(offset, origin);
    public override void SetLength(long value) => BaseStream.SetLength(value);
}
