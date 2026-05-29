namespace TorrentCs.Application.Pipelines;

public interface IStageInterrupt
{
    bool IsPauseRequested { get; }
    bool IsStopRequested { get; }
    WaitHandle PauseWaitHandle { get; }
    WaitHandle StopWaitHandle { get; }
    WaitHandle InterruptHandle { get; }
}
