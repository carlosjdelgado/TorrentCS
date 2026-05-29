using TorrentCs.Data;

namespace TorrentCs.Application;

public interface IApplicationProtocolFactory
{
    IApplicationProtocol Create(Metainfo metainfo, IBlockDataHandler dataHandler);
}
