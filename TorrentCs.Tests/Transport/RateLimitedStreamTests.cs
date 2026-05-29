using TorrentCs.Transport;

namespace TorrentCs.Tests.Transport;

public class RateLimitedStreamTests
{
    [Fact]
    public void Read_DelegatesToBaseStream()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        using var baseStream = new MemoryStream(data);
        var stream = new RateLimitedStream(baseStream, new RateLimiter());

        var buffer = new byte[5];
        stream.ReadExactly(buffer, 0, 5);

        Assert.Equal(data, buffer);
    }

    [Fact]
    public void Write_DelegatesToBaseStream()
    {
        using var baseStream = new MemoryStream();
        var stream = new RateLimitedStream(baseStream, new RateLimiter());
        var data = new byte[] { 10, 20, 30 };

        stream.Write(data, 0, data.Length);

        Assert.Equal(data, baseStream.ToArray());
    }

    [Fact]
    public void CanRead_DelegatesToBaseStream()
    {
        using var baseStream = new MemoryStream();
        var stream = new RateLimitedStream(baseStream, new RateLimiter());
        Assert.Equal(baseStream.CanRead, stream.CanRead);
    }

    [Fact]
    public void CanWrite_DelegatesToBaseStream()
    {
        using var baseStream = new MemoryStream();
        var stream = new RateLimitedStream(baseStream, new RateLimiter());
        Assert.Equal(baseStream.CanWrite, stream.CanWrite);
    }

    [Fact]
    public void CanSeek_DelegatesToBaseStream()
    {
        using var baseStream = new MemoryStream();
        var stream = new RateLimitedStream(baseStream, new RateLimiter());
        Assert.Equal(baseStream.CanSeek, stream.CanSeek);
    }

    [Fact]
    public void Position_DelegatesToBaseStream()
    {
        using var baseStream = new MemoryStream(new byte[10]);
        var stream = new RateLimitedStream(baseStream, new RateLimiter());
        stream.Position = 5;
        Assert.Equal(5, baseStream.Position);
    }

    [Fact]
    public void Seek_DelegatesToBaseStream()
    {
        using var baseStream = new MemoryStream(new byte[20]);
        var stream = new RateLimitedStream(baseStream, new RateLimiter());
        stream.Seek(10, SeekOrigin.Begin);
        Assert.Equal(10, baseStream.Position);
    }

    [Fact]
    public void Flush_DelegatesToBaseStream()
    {
        using var baseStream = new MemoryStream();
        var stream = new RateLimitedStream(baseStream, new RateLimiter());
        stream.Flush();
    }

    [Fact]
    public void BaseStream_IsExposedCorrectly()
    {
        using var baseStream = new MemoryStream();
        var stream = new RateLimitedStream(baseStream, new RateLimiter());
        Assert.Same(baseStream, stream.BaseStream);
    }

    [Fact]
    public void Limiter_IsExposedCorrectly()
    {
        using var baseStream = new MemoryStream();
        var limiter = new RateLimiter();
        var stream = new RateLimitedStream(baseStream, limiter);
        Assert.Same(limiter, stream.Limiter);
    }
}
