using System.Security.Cryptography;

namespace TorrentCs.Data;

public sealed class MetainfoBuilder
{
    private readonly string _name;
    private readonly List<(string Name, byte[] Data)> _files = new();
    private readonly List<string> _trackers = new();
    private int _pieceSize = 512 * 1024;

    public MetainfoBuilder(string name)
    {
        _name = name;
    }

    public MetainfoBuilder AddFile(string name, byte[] data)
    {
        _files.Add((name, data));
        return this;
    }

    public MetainfoBuilder WithTracker(string url)
    {
        _trackers.Add(url);
        return this;
    }

    public MetainfoBuilder WithPieceSize(int pieceSize)
    {
        _pieceSize = pieceSize;
        return this;
    }

    public Metainfo Build()
    {
        var containedFiles = _files.Select(f => new ContainedFile(f.Name, f.Data.Length)).ToList();
        using var fileHandler = new MemoryFileHandler(_files.ToDictionary(f => f.Name, f => f.Data));

        var calculator = new PieceCalculator();
        var pieces = new List<Piece>();
        calculator.ComputePieces(containedFiles, _pieceSize, fileHandler, pieces);

        var allData = _files.SelectMany(f => f.Data).ToArray();
        var infoHash = new Sha1Hash(SHA1.HashData(allData));

        return new Metainfo(_name, infoHash, containedFiles, pieces, _pieceSize, _trackers, []);
    }
}
