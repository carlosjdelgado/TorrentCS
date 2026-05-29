namespace TorrentCs.Application.Pipelines;

public sealed class StatusUpdate
{
    public StatusUpdate(DownloadState state, double progress)
    {
        State = state;
        Progress = progress;
    }

    public DownloadState State { get; }
    public double Progress { get; }

    public override string ToString() => $"[{State}] ({Progress:P0})";
}
