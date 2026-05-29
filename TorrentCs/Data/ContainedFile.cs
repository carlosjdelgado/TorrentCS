namespace TorrentCs.Data;

public class ContainedFile
{
    public ContainedFile(string name, long size)
    {
        Name = name;
        Size = size;
    }

    public string Name { get; }
    public long Size { get; }
}
