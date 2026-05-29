using System.Collections.Concurrent;

namespace TorrentCs.Engine;

public class MainLoop : IMainLoop
{
    private const int RegularTaskIntervalMs = 100;

    private readonly ConcurrentQueue<Action> _queue = new();
    private readonly AutoResetEvent _workReady = new(false);
    private readonly List<RegularTask> _regularTasks = new();
    private readonly object _regularTasksLock = new();
    private CancellationTokenSource? _cts;
    private Thread? _thread;
    private Timer? _timer;

    public bool IsRunning => _cts is { IsCancellationRequested: false };

    public void Start()
    {
        _cts = new CancellationTokenSource();
        _thread = new Thread(Loop) { Name = "MainLoop", IsBackground = true };
        _thread.Start();
        _timer = new Timer(_ => RunRegularTasks(), null,
            RegularTaskIntervalMs, RegularTaskIntervalMs);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _timer?.Dispose();
        AddTask(() => { }); // wake the loop
    }

    public void AddTask(Action task)
    {
        _queue.Enqueue(task);
        _workReady.Set();
    }

    public IRegularTask AddRegularTask(Action task)
    {
        var regular = new RegularTask(task, this);
        lock (_regularTasksLock)
            _regularTasks.Add(regular);
        return regular;
    }

    private void Loop()
    {
        while (_cts is { IsCancellationRequested: false })
        {
            while (_queue.TryDequeue(out var task))
                task();
            _workReady.WaitOne(100);
        }
    }

    private void RunRegularTasks()
    {
        lock (_regularTasksLock)
        {
            foreach (var task in _regularTasks)
                task.Run();
        }
    }

    private void RemoveRegularTask(RegularTask task)
    {
        lock (_regularTasksLock)
            _regularTasks.Remove(task);
    }

    private sealed class RegularTask : IRegularTask
    {
        private readonly Action _action;
        private readonly MainLoop _loop;

        public RegularTask(Action action, MainLoop loop)
        {
            _action = action;
            _loop = loop;
        }

        public void Run() => _action();

        public void Dispose() => _loop.RemoveRegularTask(this);
    }
}
