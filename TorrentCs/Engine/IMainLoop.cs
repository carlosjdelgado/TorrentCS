namespace TorrentCs.Engine;

public interface IMainLoop
{
    bool IsRunning { get; }

    void Start();
    void Stop();
    void AddTask(Action task);
    IRegularTask AddRegularTask(Action task);
}
