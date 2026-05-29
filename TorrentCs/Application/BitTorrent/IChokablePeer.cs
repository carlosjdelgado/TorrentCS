namespace TorrentCs.Application.BitTorrent;

/// <summary>
/// The subset of peer state and behaviour the <see cref="ChokingManager"/> needs to decide which
/// peers to choke or unchoke.
/// </summary>
public interface IChokablePeer
{
    /// <summary>Whether the remote peer wants to download from us.</summary>
    bool IsInterestedInRemotePeer { get; }

    /// <summary>Whether we are currently choking the remote peer (not serving it data).</summary>
    bool IsChokingRemotePeer { get; }

    /// <summary>How fast, in bytes per second, this peer is currently uploading to us.</summary>
    long DownloadRate();

    /// <summary>Stop serving data to the peer.</summary>
    void Choke();

    /// <summary>Allow the peer to request data from us.</summary>
    void Unchoke();
}
