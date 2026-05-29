using TorrentCs.Application.BitTorrent;
using TorrentCs.Data;

namespace TorrentCs;

public interface ITorrentClient : IDisposable
{
    IReadOnlyCollection<TorrentDownload> Downloads { get; }
    PeerId LocalPeerId { get; }

    TorrentDownload Add(Metainfo metainfo, string downloadDirectory);
    TorrentDownload Add(Stream torrentStream, string downloadDirectory);
    TorrentDownload Add(string torrentFilePath, string downloadDirectory);

    /// <summary>Adds a torrent from a magnet link, fetching its metadata from peers via BEP 9.</summary>
    TorrentDownload Add(MagnetLink magnet, string downloadDirectory);

    /// <summary>
    /// Adds a torrent known only by its info-hash (e.g. from a magnet link), fetching the metadata
    /// from peers via BEP 9 before downloading the data.
    /// </summary>
    TorrentDownload Add(Sha1Hash infoHash, IEnumerable<string> trackers, string downloadDirectory);
}
