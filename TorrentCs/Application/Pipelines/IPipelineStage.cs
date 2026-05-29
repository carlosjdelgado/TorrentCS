namespace TorrentCs.Application.Pipelines;

public interface IPipelineStage
{
    void Run(IStageInterrupt interrupt, IProgress<StatusUpdate> progress);
}
