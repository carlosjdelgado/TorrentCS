using TorrentCs.Application.BitTorrent;

namespace TorrentCs.Modularity;

public interface IPeerContext : ITorrentContext
{
    IPeer Peer { get; }

    T GetValue<T>(string key);
    void SetValue<T>(string key, T value);
    void RegisterMessageHandler(byte messageId);
    void SendMessage(byte messageId, byte[] data);
}
