namespace TorrentCs.Application.Pipelines;

public class StageInterrupt : IStageInterrupt
{
    private readonly ManualResetEvent _pause = new(false);
    private readonly ManualResetEvent _stop = new(false);
    private readonly ManualResetEvent _interrupt = new(false);

    public bool IsPauseRequested => _pause.WaitOne(0);
    public bool IsStopRequested => _stop.WaitOne(0);
    public WaitHandle PauseWaitHandle => _pause;
    public WaitHandle StopWaitHandle => _stop;
    public WaitHandle InterruptHandle => _interrupt;

    public void Pause()
    {
        _pause.Set();
        _interrupt.Set();
    }

    public void Resume()
    {
        _pause.Reset();
        _interrupt.Set();
    }

    public void Stop()
    {
        _stop.Set();
        _interrupt.Set();
    }

    public void Reset()
    {
        _pause.Reset();
        _stop.Reset();
        _interrupt.Reset();
    }
}
