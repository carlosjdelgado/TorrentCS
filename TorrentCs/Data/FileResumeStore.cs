namespace TorrentCs.Data;

/// <summary>
/// Stores resume state as a small binary file (<c>.{infoHash}.resume</c>) inside the torrent's
/// download directory. Format: magic, version, info-hash, piece count, then a piece bitfield.
/// </summary>
public sealed class FileResumeStore : IResumeStore
{
    private static readonly byte[] Magic = "TCRS"u8.ToArray();
    private const byte Version = 1;

    private readonly object _lock = new();

    public ResumeData? Load(string directory, Sha1Hash infoHash, int pieceCount)
    {
        var path = ResumeFilePath(directory, infoHash);
        lock (_lock)
        {
            if (!File.Exists(path)) return null;

            try
            {
                using var stream = File.OpenRead(path);
                using var reader = new BinaryReader(stream);

                if (!reader.ReadBytes(Magic.Length).AsSpan().SequenceEqual(Magic)) return null;
                if (reader.ReadByte() != Version) return null;
                if (!reader.ReadBytes(Sha1Hash.Length).SequenceEqual(infoHash.Value)) return null;
                if (reader.ReadInt32() != pieceCount) return null;

                var bitfield = reader.ReadBytes((pieceCount + 7) / 8);
                var completed = new List<int>();
                for (int i = 0; i < pieceCount; i++)
                {
                    if ((bitfield[i / 8] & (1 << (7 - (i % 8)))) != 0)
                        completed.Add(i);
                }

                return new ResumeData(infoHash, pieceCount, completed);
            }
            catch
            {
                return null; // corrupt or truncated resume file → fall back to full verification
            }
        }
    }

    public void Save(string directory, ResumeData data)
    {
        var path = ResumeFilePath(directory, data.InfoHash);
        var bitfield = new byte[(data.PieceCount + 7) / 8];
        foreach (var index in data.CompletedPieces)
            bitfield[index / 8] |= (byte)(1 << (7 - (index % 8)));

        lock (_lock)
        {
            Directory.CreateDirectory(directory);

            // Write to a temp file and move into place so a crash never leaves a half-written file.
            var tempPath = path + ".tmp";
            using (var stream = File.Create(tempPath))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(Magic);
                writer.Write(Version);
                writer.Write(data.InfoHash.Value);
                writer.Write(data.PieceCount);
                writer.Write(bitfield);
            }
            File.Move(tempPath, path, overwrite: true);
        }
    }

    private static string ResumeFilePath(string directory, Sha1Hash infoHash)
        => Path.Combine(directory, $".{Convert.ToHexString(infoHash.Value)}.resume");
}
