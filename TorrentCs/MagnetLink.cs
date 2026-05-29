using TorrentCs.Data;

namespace TorrentCs;

/// <summary>
/// A parsed magnet link (<c>magnet:?xt=urn:btih:...&amp;tr=...&amp;dn=...</c>). Only the BitTorrent
/// info-hash (<c>xt=urn:btih:</c>), trackers (<c>tr</c>) and display name (<c>dn</c>) are used; other
/// parameters are ignored.
/// </summary>
public sealed class MagnetLink
{
    private const string Scheme = "magnet:?";
    private const string BtihPrefix = "urn:btih:";
    private const string Base32Alphabet = "ABCDEFGHIJKLMNOPQRSTUVWXYZ234567";

    public MagnetLink(Sha1Hash infoHash, IReadOnlyList<string> trackers, string? displayName)
    {
        InfoHash = infoHash;
        Trackers = trackers;
        DisplayName = displayName;
    }

    public Sha1Hash InfoHash { get; }

    public IReadOnlyList<string> Trackers { get; }

    public string? DisplayName { get; }

    public static bool IsMagnetLink(string value) =>
        value.StartsWith("magnet:", StringComparison.OrdinalIgnoreCase);

    public static MagnetLink Parse(string uri)
    {
        if (!TryParse(uri, out var magnet))
            throw new FormatException($"Not a valid magnet link: {uri}");
        return magnet!;
    }

    public static bool TryParse(string uri, out MagnetLink? magnet)
    {
        magnet = null;
        if (uri is null || !uri.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase))
            return false;

        Sha1Hash? infoHash = null;
        var trackers = new List<string>();
        string? displayName = null;

        foreach (var pair in uri[Scheme.Length..].Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            int eq = pair.IndexOf('=');
            if (eq < 0) continue;
            string key = pair[..eq];
            string value = Uri.UnescapeDataString(pair[(eq + 1)..]);

            switch (key)
            {
                case "xt" when infoHash is null && value.StartsWith(BtihPrefix, StringComparison.OrdinalIgnoreCase):
                    if (!TryParseInfoHash(value[BtihPrefix.Length..], out infoHash))
                        return false;
                    break;
                case "tr":
                    trackers.Add(value);
                    break;
                case "dn":
                    displayName = value;
                    break;
            }
        }

        if (infoHash is null)
            return false;

        magnet = new MagnetLink(infoHash, trackers, displayName);
        return true;
    }

    private static bool TryParseInfoHash(string value, out Sha1Hash? infoHash)
    {
        infoHash = null;
        try
        {
            byte[] bytes = value.Length switch
            {
                40 => Convert.FromHexString(value),         // hex-encoded (BEP 9)
                32 => DecodeBase32(value),                  // base32-encoded (BEP 9)
                _ => throw new FormatException("info-hash must be 40 hex or 32 base32 characters"),
            };
            infoHash = new Sha1Hash(bytes);
            return true;
        }
        catch (FormatException)
        {
            return false;
        }
    }

    private static byte[] DecodeBase32(string value)
    {
        value = value.TrimEnd('=').ToUpperInvariant();
        var output = new byte[value.Length * 5 / 8];
        int buffer = 0, bits = 0, index = 0;

        foreach (char c in value)
        {
            int digit = Base32Alphabet.IndexOf(c);
            if (digit < 0) throw new FormatException($"Invalid base32 character '{c}'.");

            buffer = (buffer << 5) | digit;
            bits += 5;
            if (bits >= 8)
            {
                output[index++] = (byte)((buffer >> (bits - 8)) & 0xFF);
                bits -= 8;
            }
        }

        return output;
    }
}
