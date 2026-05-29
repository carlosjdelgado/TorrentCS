using System.Net;
using TorrentCs.Application.BitTorrent;

namespace TorrentCs;

public class TorrentClientSettings
{
    public TorrentClientSettings()
    {
        PeerId = PeerId.CreateNew();
    }

    public PeerId PeerId { get; set; }
    public int ListenPort { get; set; } = 6881;
    public bool FindAvailablePort { get; set; }
    public IPAddress AdapterAddress { get; set; } = IPAddress.Any;
}
