using TorrentCs.Data;

namespace TorrentCs.Engine;

public interface ITorrentPipelineRunner
{
    Metainfo Description { get; }
    DownloadState State { get; }
}
