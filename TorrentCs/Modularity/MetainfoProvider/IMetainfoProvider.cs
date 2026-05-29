using TorrentCs.Data;

namespace TorrentCs.Modularity.MetainfoProvider;

public interface IMetainfoProvider
{
    Task<Metainfo> GetMetainfo(ITorrentContext context, CancellationToken ct);
}
