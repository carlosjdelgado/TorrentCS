namespace TorrentCs.Application.BitTorrent;

/// <summary>
/// Decides which connected peers to unchoke, following the classic BitTorrent strategy: a fixed
/// number of upload slots go to the peers that upload to us fastest (tit-for-tat), and one slot is
/// periodically given to a random peer (optimistic unchoke) to discover better peers.
/// </summary>
public class ChokingManager
{
    public const int DefaultMaxUnchoked = 4;

    /// <summary>Run an optimistic unchoke every Nth update (≈30s when updated every 10s).</summary>
    private const int OptimisticInterval = 3;

    private readonly int _maxUnchoked;
    private readonly Random _random;
    private int _updateCount;

    public ChokingManager(int maxUnchoked = DefaultMaxUnchoked, Random? random = null)
    {
        if (maxUnchoked < 1)
            throw new ArgumentOutOfRangeException(nameof(maxUnchoked), "At least one upload slot is required.");
        _maxUnchoked = maxUnchoked;
        _random = random ?? Random.Shared;
    }

    /// <summary>
    /// Recalculates the unchoked set and applies the resulting choke/unchoke transitions to peers.
    /// Intended to be called periodically (≈ every 10 seconds).
    /// </summary>
    public void Update(IReadOnlyList<IChokablePeer> peers)
    {
        // First rounds fill slots purely by rate; an optimistic unchoke kicks in every Nth round.
        bool optimistic = ++_updateCount % OptimisticInterval == 0;
        var unchoked = SelectUnchoked(peers, optimistic);

        foreach (var peer in peers)
        {
            if (unchoked.Contains(peer))
            {
                if (peer.IsChokingRemotePeer) peer.Unchoke();
            }
            else if (!peer.IsChokingRemotePeer)
            {
                peer.Choke();
            }
        }
    }

    /// <summary>
    /// Immediately unchokes interested peers while free upload slots remain, without choking anyone
    /// or disturbing the periodic rotation. Lets a newly-interested peer start downloading without
    /// waiting for the next <see cref="Update"/>.
    /// </summary>
    public void FillFreeSlots(IReadOnlyList<IChokablePeer> peers)
    {
        int unchokedCount = peers.Count(p => !p.IsChokingRemotePeer);
        if (unchokedCount >= _maxUnchoked) return;

        var candidates = peers
            .Where(p => p.IsInterestedInRemotePeer && p.IsChokingRemotePeer)
            .OrderByDescending(p => p.DownloadRate())
            .Take(_maxUnchoked - unchokedCount);

        foreach (var peer in candidates)
            peer.Unchoke();
    }

    /// <summary>
    /// Pure selection: the interested peers that should be unchoked. Exposed for testing.
    /// </summary>
    public ISet<IChokablePeer> SelectUnchoked(IReadOnlyList<IChokablePeer> peers, bool optimistic)
    {
        // Only peers interested in us are worth an upload slot.
        var interested = peers
            .Where(p => p.IsInterestedInRemotePeer)
            .OrderByDescending(p => p.DownloadRate())
            .ToList();

        int regularSlots = optimistic ? Math.Max(0, _maxUnchoked - 1) : _maxUnchoked;
        var selected = new HashSet<IChokablePeer>(interested.Take(regularSlots));

        if (optimistic)
        {
            var candidates = interested.Skip(regularSlots).ToList();
            if (candidates.Count > 0)
                selected.Add(candidates[_random.Next(candidates.Count)]);
        }

        return selected;
    }
}
