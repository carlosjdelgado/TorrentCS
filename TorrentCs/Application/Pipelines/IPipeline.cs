namespace TorrentCs.Application.Pipelines;

public interface IPipeline
{
    void Run(IStageInterrupt interrupt, IProgress<StatusUpdate> progress);
}
