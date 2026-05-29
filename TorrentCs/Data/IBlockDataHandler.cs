namespace TorrentCs.Data;

public interface IBlockDataHandler
{
    Metainfo Metainfo { get; }

    byte[] ReadBlockData(long offset, int length);
    bool TryReadBlockData(long offset, int length, out byte[] data);
    void WriteBlockData(long offset, byte[] data);
    void Flush();
}
