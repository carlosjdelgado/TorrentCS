using TorrentCs.Data;

namespace TorrentCs.Tests.Data;

public class ContainedFileTests
{
    [Fact]
    public void Properties_AreSetCorrectly()
    {
        var file = new ContainedFile("folder/test.txt", 1024);
        Assert.Equal("folder/test.txt", file.Name);
        Assert.Equal(1024L, file.Size);
    }
}
