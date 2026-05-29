using System.Net;

namespace TorrentCs.Transport;

public class BigEndianBinaryReader : BinaryReader
{
    public BigEndianBinaryReader(Stream stream) : base(stream) { }

    public override short ReadInt16() => IPAddress.NetworkToHostOrder(base.ReadInt16());

    public override ushort ReadUInt16() => (ushort)IPAddress.NetworkToHostOrder((short)base.ReadUInt16());

    public override int ReadInt32() => IPAddress.NetworkToHostOrder(base.ReadInt32());

    public override long ReadInt64() => IPAddress.NetworkToHostOrder(base.ReadInt64());
}
