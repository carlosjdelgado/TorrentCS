namespace TorrentCs.Modularity;

public interface IMessageReceivedContext : IPeerContext
{
    int MessageId { get; }
    int MessageLength { get; }
    BinaryReader Reader { get; }
}
