using TorrentCs;
using TorrentCs.Data;

namespace TorrentCs.Tests;

public class MagnetLinkTests
{
    private const string Hex = "0123456789abcdef0123456789abcdef01234567";

    [Fact]
    public void Parse_HexInfoHash_DecodesTo20Bytes()
    {
        var magnet = MagnetLink.Parse($"magnet:?xt=urn:btih:{Hex}");
        Assert.Equal(new Sha1Hash(Convert.FromHexString(Hex)), magnet.InfoHash);
    }

    [Fact]
    public void Parse_Base32InfoHash_Decodes()
    {
        // 32 'A's in base32 decode to 20 zero bytes.
        var magnet = MagnetLink.Parse("magnet:?xt=urn:btih:AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA");
        Assert.Equal(new Sha1Hash(new byte[20]), magnet.InfoHash);
    }

    [Fact]
    public void Parse_ExtractsTrackersAndDisplayName()
    {
        var magnet = MagnetLink.Parse(
            $"magnet:?xt=urn:btih:{Hex}&dn=Some%20File&tr=http%3A%2F%2Ftr1%2Fannounce&tr=udp%3A%2F%2Ftr2%3A80");

        Assert.Equal("Some File", magnet.DisplayName);
        Assert.Equal(["http://tr1/announce", "udp://tr2:80"], magnet.Trackers);
    }

    [Fact]
    public void Parse_NoTrackers_YieldsEmptyList()
    {
        var magnet = MagnetLink.Parse($"magnet:?xt=urn:btih:{Hex}");
        Assert.Empty(magnet.Trackers);
        Assert.Null(magnet.DisplayName);
    }

    [Theory]
    [InlineData("magnet:?xt=urn:btih:0123456789abcdef0123456789abcdef01234567", true)]
    [InlineData("MAGNET:?xt=urn:btih:0123456789abcdef0123456789abcdef01234567", true)]
    [InlineData("/path/to/file.torrent", false)]
    public void IsMagnetLink_DetectsScheme(string value, bool expected)
    {
        Assert.Equal(expected, MagnetLink.IsMagnetLink(value));
    }

    [Theory]
    [InlineData("/path/to/file.torrent")]                       // not a magnet
    [InlineData("magnet:?dn=NoHash")]                            // no xt
    [InlineData("magnet:?xt=urn:btih:tooShort")]                 // invalid hash length
    [InlineData("magnet:?xt=urn:btih:zzzz567890abcdef0123456789abcdef01234567")] // non-hex chars
    public void TryParse_Invalid_ReturnsFalse(string value)
    {
        Assert.False(MagnetLink.TryParse(value, out var magnet));
        Assert.Null(magnet);
    }
}
