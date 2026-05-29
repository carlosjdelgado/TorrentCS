using System.Net;

namespace TorrentCs.Transport;

public class BigEndianBinaryWriter : BinaryWriter
{
    public BigEndianBinaryWriter(Stream stream) : base(stream) { }

    public override void Write(short value) => base.Write(IPAddress.HostToNetworkOrder(value));

    public override void Write(ushort value)
    {
        base.Write((byte)(value >> 8));
        base.Write((byte)value);
    }

    public override void Write(int value) => base.Write(IPAddress.HostToNetworkOrder(value));

    public override void Write(long value) => base.Write(IPAddress.HostToNetworkOrder(value));
}
