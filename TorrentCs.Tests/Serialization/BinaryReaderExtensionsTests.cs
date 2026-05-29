using System.Net;
using TorrentCs.Serialization;

namespace TorrentCs.Tests.Serialization;

public class BinaryReaderExtensionsTests
{
    [Fact]
    public void ReadIpV4EndPoint_ParsesAddressCorrectly()
    {
        // 192.168.1.100 → bytes 192, 168, 1, 100
        // port 6969 big-endian → 0x1B, 0x39
        var reader = MakeReader(192, 168, 1, 100, 0x1B, 0x39);
        var endpoint = reader.ReadIpV4EndPoint();

        Assert.Equal(IPAddress.Parse("192.168.1.100"), endpoint.Address);
        Assert.Equal(6969, endpoint.Port);
    }

    [Fact]
    public void ReadIpV4EndPoint_PortInNetworkByteOrder()
    {
        // port 80 = 0x00, 0x50 in big-endian
        var reader = MakeReader(10, 0, 0, 1, 0x00, 0x50);
        var endpoint = reader.ReadIpV4EndPoint();

        Assert.Equal(80, endpoint.Port);
    }

    [Fact]
    public void ReadIpV4EndPoint_LoopbackAddress()
    {
        // 127.0.0.1, port 1234 = 0x04, 0xD2
        var reader = MakeReader(127, 0, 0, 1, 0x04, 0xD2);
        var endpoint = reader.ReadIpV4EndPoint();

        Assert.Equal(IPAddress.Loopback, endpoint.Address);
        Assert.Equal(1234, endpoint.Port);
    }

    [Fact]
    public void ReadIpV4EndPoint_MaxPort()
    {
        // port 65535 = 0xFF, 0xFF
        var reader = MakeReader(1, 2, 3, 4, 0xFF, 0xFF);
        var endpoint = reader.ReadIpV4EndPoint();

        Assert.Equal(65535, endpoint.Port);
    }

    [Fact]
    public void ReadIpV4EndPoint_MultipleConsecutive()
    {
        // Two peers back to back
        var reader = MakeReader(
            1, 2, 3, 4, 0x00, 0x50,   // 1.2.3.4:80
            5, 6, 7, 8, 0x1B, 0x39    // 5.6.7.8:6969
        );

        var ep1 = reader.ReadIpV4EndPoint();
        var ep2 = reader.ReadIpV4EndPoint();

        Assert.Equal(80, ep1.Port);
        Assert.Equal(IPAddress.Parse("1.2.3.4"), ep1.Address);
        Assert.Equal(6969, ep2.Port);
        Assert.Equal(IPAddress.Parse("5.6.7.8"), ep2.Address);
    }

    private static BinaryReader MakeReader(params byte[] bytes)
        => new(new MemoryStream(bytes));
}
