namespace TorrentCs.Data;

internal class BlockComparer : IComparer<Block>
{
    public int Compare(Block? x, Block? y)
    {
        if (x is null || y is null) return 0;
        return x.Offset.CompareTo(y.Offset);
    }
}
