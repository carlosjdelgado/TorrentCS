using System.Security.Cryptography;
using BencodeNET.Objects;
using BencodeNET.Parsing;
using TorrentCs.Data;

namespace TorrentCs.TorrentParsers;

public static class TorrentParser
{
    public static Metainfo ReadFromStream(Stream stream)
    {
        var parser = new BencodeParser();
        var torrent = parser.Parse<BDictionary>(stream);
        var info = (BDictionary)torrent["info"];

        using var infoStream = new MemoryStream();
        info.EncodeTo(infoStream);
        var rawInfoDict = infoStream.ToArray();
        var infoHash = new Sha1Hash(SHA1.HashData(rawInfoDict));

        var name = ((BString)info["name"]).ToString();
        int pieceSize = (int)((BNumber)info["piece length"]).Value;
        var piecesRaw = ((BString)info["pieces"]).Value.ToArray();

        var files = ParseFiles(info, name);
        var pieces = BuildPieces(piecesRaw, pieceSize, files.Sum(f => f.Size));
        var trackers = ParseTrackers(torrent);

        return new Metainfo(name, infoHash, files, pieces, pieceSize, trackers, rawInfoDict);
    }

    private static List<ContainedFile> ParseFiles(BDictionary info, string name)
    {
        if (info.ContainsKey("files"))
        {
            var fileList = (BList)info["files"];
            return fileList
                .Cast<BDictionary>()
                .Select(f =>
                {
                    long size = ((BNumber)f["length"]).Value;
                    var parts = ((BList)f["path"])
                        .Cast<BString>()
                        .Select(s => s.ToString());
                    var relativePath = Path.Combine([name, ..parts]);
                    return new ContainedFile(relativePath, size);
                })
                .ToList();
        }

        long fileSize = ((BNumber)info["length"]).Value;
        return [new ContainedFile(name, fileSize)];
    }

    private static List<Piece> BuildPieces(byte[] piecesRaw, int pieceSize, long totalSize)
    {
        int count = piecesRaw.Length / Sha1Hash.Length;
        var pieces = new List<Piece>(count);

        for (int i = 0; i < count; i++)
        {
            var hashBytes = piecesRaw.AsSpan(i * Sha1Hash.Length, Sha1Hash.Length).ToArray();
            var hash = new Sha1Hash(hashBytes);
            long remaining = totalSize - (long)i * pieceSize;
            int size = (int)Math.Min(pieceSize, remaining);
            pieces.Add(new Piece(i, size, hash));
        }

        return pieces;
    }

    private static List<string> ParseTrackers(BDictionary torrent)
    {
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var trackers = new List<string>();

        void Add(string url)
        {
            if (seen.Add(url)) trackers.Add(url);
        }

        if (torrent.ContainsKey("announce"))
            Add(((BString)torrent["announce"]).ToString());

        if (torrent.ContainsKey("announce-list"))
        {
            foreach (var tier in ((BList)torrent["announce-list"]).Cast<BList>())
                foreach (var t in tier.Cast<BString>())
                    Add(t.ToString());
        }

        return trackers;
    }
}
