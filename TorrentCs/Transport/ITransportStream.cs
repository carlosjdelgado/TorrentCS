namespace TorrentCs.Transport;

public interface ITransportStream
{
    bool IsConnected { get; }
    string DisplayAddress { get; }
    object Address { get; }
    Stream Stream { get; }

    Task Connect();
    void Disconnect();
}
