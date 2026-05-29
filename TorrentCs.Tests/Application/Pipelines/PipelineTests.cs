using Microsoft.Extensions.Logging.Abstractions;
using TorrentCs.Application.Pipelines;

namespace TorrentCs.Tests.Application.Pipelines;

public class PipelineTests
{
    [Fact]
    public void Run_ExecutesStagesInOrder()
    {
        var order = new List<int>();
        var stages = new IPipelineStage[]
        {
            new OrderedStage(order, 1),
            new OrderedStage(order, 2),
            new OrderedStage(order, 3),
        };

        var pipeline = new Pipeline(NullLogger<Pipeline>.Instance, stages);
        pipeline.Run(new StageInterrupt(), new Progress<StatusUpdate>(_ => { }));

        Assert.Equal([1, 2, 3], order);
    }

    [Fact]
    public void Run_StageFails_ReportsFailed()
    {
        StatusUpdate? lastUpdate = null;
        var stages = new IPipelineStage[] { new FailingStage() };
        var pipeline = new Pipeline(NullLogger<Pipeline>.Instance, stages);

        pipeline.Run(new StageInterrupt(), new SyncProgress<StatusUpdate>(u => lastUpdate = u));

        Assert.NotNull(lastUpdate);
        Assert.Equal(DownloadState.Failed, lastUpdate!.State);
    }

    [Fact]
    public void Run_StopRequested_HaltsAfterStage()
    {
        var order = new List<int>();
        var interrupt = new StageInterrupt();
        var stages = new IPipelineStage[]
        {
            new OrderedStage(order, 1, afterRun: () => interrupt.Stop()),
            new OrderedStage(order, 2),
        };

        var pipeline = new Pipeline(NullLogger<Pipeline>.Instance, stages);
        pipeline.Run(interrupt, new Progress<StatusUpdate>(_ => { }));

        Assert.Single(order); // stage 2 should not run
    }

    [Fact]
    public void StageInterrupt_Pause_IsPauseRequested()
    {
        var interrupt = new StageInterrupt();
        interrupt.Pause();
        Assert.True(interrupt.IsPauseRequested);
    }

    [Fact]
    public void StageInterrupt_Stop_IsStopRequested()
    {
        var interrupt = new StageInterrupt();
        interrupt.Stop();
        Assert.True(interrupt.IsStopRequested);
    }

    [Fact]
    public void StageInterrupt_Reset_ClearsAll()
    {
        var interrupt = new StageInterrupt();
        interrupt.Pause();
        interrupt.Stop();
        interrupt.Reset();
        Assert.False(interrupt.IsPauseRequested);
        Assert.False(interrupt.IsStopRequested);
    }

    [Fact]
    public void StatusUpdate_ToString_ContainsState()
    {
        var update = new StatusUpdate(DownloadState.Downloading, 0.5);
        Assert.Contains("Downloading", update.ToString());
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────

    private sealed class SyncProgress<T>(Action<T> action) : IProgress<T>
    {
        public void Report(T value) => action(value);
    }

    private sealed class OrderedStage(List<int> order, int index, Action? afterRun = null) : IPipelineStage
    {
        public void Run(IStageInterrupt interrupt, IProgress<StatusUpdate> progress)
        {
            order.Add(index);
            afterRun?.Invoke();
        }
    }

    private sealed class FailingStage : IPipelineStage
    {
        public void Run(IStageInterrupt interrupt, IProgress<StatusUpdate> progress) =>
            throw new InvalidOperationException("Simulated stage failure");
    }
}
