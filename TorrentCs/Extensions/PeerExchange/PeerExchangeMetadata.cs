namespace TorrentCs.Extensions.PeerExchange;

/// <summary>
/// Per-peer state for Peer Exchange: when we last sent this peer a PEX message and the snapshot of
/// connected peers at that time, so the next message can carry only the added/dropped delta.
/// </summary>
internal sealed class PeerExchangeMetadata
{
    public const string Key = "ut_pex.metadata";

    public DateTime LastMessageDate { get; set; } = DateTime.MinValue;

    public IReadOnlyCollection<string> ConnectedPeersSnapshot { get; set; } = [];
}
