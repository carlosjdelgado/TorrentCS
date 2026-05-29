using System.Net;

namespace TorrentCs.Serialization;

public static class BinaryReaderExtensions
{
    /// <summary>
    /// Reads a 4-byte IPv4 address followed by a 2-byte port in network (big-endian) byte order.
    /// </summary>
    public static IPEndPoint ReadIpV4EndPoint(this BinaryReader reader)
    {
        var addressBytes = reader.ReadBytes(4);
        var portBytes = reader.ReadBytes(2);
        int port = (portBytes[0] << 8) | portBytes[1];
        return new IPEndPoint(new IPAddress(addressBytes), port);
    }
}
