using Microsoft.Extensions.Logging;

namespace TorrentCs.Application.Pipelines;

public class Pipeline : IPipeline
{
    private readonly ILogger<Pipeline> _logger;
    private readonly IReadOnlyList<IPipelineStage> _stages;

    public Pipeline(ILogger<Pipeline> logger, IEnumerable<IPipelineStage> stages)
    {
        _logger = logger;
        _stages = stages.ToList().AsReadOnly();
    }

    public void Run(IStageInterrupt interrupt, IProgress<StatusUpdate> progress)
    {
        foreach (var stage in _stages)
        {
            _logger.LogDebug("Starting pipeline stage {Stage}", stage.GetType().Name);
            try
            {
                stage.Run(interrupt, progress);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Pipeline stage {Stage} failed", stage.GetType().Name);
                progress.Report(new StatusUpdate(DownloadState.Failed, 0));
                return;
            }
            _logger.LogDebug("Finished pipeline stage {Stage}", stage.GetType().Name);

            if (interrupt.IsStopRequested)
                return;
        }
    }
}
