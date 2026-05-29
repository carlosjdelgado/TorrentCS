namespace TorrentCs.Transport;

public interface ITransportProtocol
{
    event Action<AcceptConnectionEventArgs>? AcceptConnectionHandler;

    IReadOnlyCollection<ITransportStream> Streams { get; }

    void Start();
    void Stop();
}
