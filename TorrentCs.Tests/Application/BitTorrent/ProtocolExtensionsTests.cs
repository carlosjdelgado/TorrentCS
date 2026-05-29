using TorrentCs.Application.BitTorrent;

namespace TorrentCs.Tests.Application.BitTorrent;

public class ProtocolExtensionsTests
{
    [Fact]
    public void AllZeroBytes_NoExtensions()
    {
        var reserved = new byte[8];
        var ext = ProtocolExtensions.DetermineSupportedProtocolExtensions(reserved);
        Assert.Equal(ProtocolExtension.None, ext);
    }

    [Fact]
    public void DhtBit_Byte7Bit0_DetectedCorrectly()
    {
        var reserved = new byte[8];
        reserved[7] = 0x01;
        var ext = ProtocolExtensions.DetermineSupportedProtocolExtensions(reserved);
        Assert.True(ext.HasFlag(ProtocolExtension.Dht));
    }

    [Fact]
    public void FastPeersBit_Byte7Bit2_DetectedCorrectly()
    {
        var reserved = new byte[8];
        reserved[7] = 0x04;
        var ext = ProtocolExtensions.DetermineSupportedProtocolExtensions(reserved);
        Assert.True(ext.HasFlag(ProtocolExtension.FastPeers));
    }

    [Fact]
    public void ExtensionProtocolBit_Byte5Bit4_DetectedCorrectly()
    {
        var reserved = new byte[8];
        reserved[5] = 0x10;
        var ext = ProtocolExtensions.DetermineSupportedProtocolExtensions(reserved);
        Assert.True(ext.HasFlag(ProtocolExtension.ExtensionProtocol));
    }

    [Fact]
    public void MultipleExtensions_DetectedTogether()
    {
        var reserved = new byte[8];
        reserved[7] = 0x01 | 0x04;
        var ext = ProtocolExtensions.DetermineSupportedProtocolExtensions(reserved);
        Assert.True(ext.HasFlag(ProtocolExtension.Dht));
        Assert.True(ext.HasFlag(ProtocolExtension.FastPeers));
    }

    [Fact]
    public void TooShortArray_ReturnsNone()
    {
        var ext = ProtocolExtensions.DetermineSupportedProtocolExtensions(new byte[4]);
        Assert.Equal(ProtocolExtension.None, ext);
    }
}
