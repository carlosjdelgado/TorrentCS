namespace TorrentCs.Application.Pipelines;

public interface IPipelineFactory
{
    IPipeline CreatePipeline(params object[] additionalDependencies);
}
