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
}
