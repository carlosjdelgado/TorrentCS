using System.Reflection;
using System.Text;

namespace TorrentCs.Application.BitTorrent;

public sealed class PeerId
{
    public const int Length = 20;

    private static readonly IReadOnlyDictionary<string, string> KnownClients = LoadKnownClients();

    public PeerId(byte[] value)
    {
        if (value.Length != Length)
            throw new ArgumentException($"PeerId must be {Length} bytes.", nameof(value));
        Value = value;

        if (IsAzureusFormat(value))
        {
            var prefix = Encoding.ASCII.GetString(value, 1, 2);
            ClientName = KnownClients.TryGetValue(prefix, out var name) ? name : prefix;
            if (int.TryParse(Encoding.ASCII.GetString(value, 3, 4), out int version))
                ClientVersion = version;
        }
    }

    public byte[] Value { get; }
    public string? ClientName { get; }
    public int? ClientVersion { get; }

    public static PeerId CreateNew()
    {
        var bytes = new byte[Length];
        var prefix = Encoding.ASCII.GetBytes("-TC0001-");
        Array.Copy(prefix, bytes, prefix.Length);
        Random.Shared.NextBytes(bytes.AsSpan(prefix.Length));
        return new PeerId(bytes);
    }

    public override string ToString() => Encoding.UTF8.GetString(Value);

    private static bool IsAzureusFormat(byte[] value) =>
        value.Length >= 8 && value[0] == '-' && value[7] == '-';

    private static IReadOnlyDictionary<string, string> LoadKnownClients()
    {
        var dict = new Dictionary<string, string>(StringComparer.Ordinal);
        try
        {
            var asm = Assembly.GetExecutingAssembly();
            using var stream = asm.GetManifestResourceStream("TorrentCs.Transport.ClientPeerIds.txt");
            if (stream is null) return dict;
            using var reader = new StreamReader(stream);
            string? line;
            while ((line = reader.ReadLine()) is not null)
            {
                line = line.Trim();
                if (line.Length == 2) dict[line] = line;
            }
        }
        catch { /* best-effort */ }
        return dict;
    }
}
