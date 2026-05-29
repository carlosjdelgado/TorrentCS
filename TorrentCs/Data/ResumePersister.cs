using TorrentCs.Data.Pieces;

namespace TorrentCs.Data;

/// <summary>
/// Keeps a torrent's resume file up to date as pieces complete. Saves are throttled to avoid
/// hammering the disk during a verification burst; the final state is flushed on dispose.
/// </summary>
public sealed class ResumePersister : IDisposable
{
    private readonly IResumeStore _store;
    private readonly IPieceDataHandler _dataHandler;
    private readonly string _directory;
    private readonly TimeSpan _minInterval;
    private readonly object _lock = new();
    private DateTime _lastSave = DateTime.MinValue;
    private bool _pending;

    public ResumePersister(
        IResumeStore store,
        IPieceDataHandler dataHandler,
        string directory,
        TimeSpan? minSaveInterval = null)
    {
        _store = store;
        _dataHandler = dataHandler;
        _directory = directory;
        _minInterval = minSaveInterval ?? TimeSpan.FromSeconds(1);
        _dataHandler.PieceCompleted += OnPieceCompleted;
    }

    public void Dispose()
    {
        _dataHandler.PieceCompleted -= OnPieceCompleted;
        lock (_lock)
        {
            if (_pending) SaveLocked();
        }
    }

    private void OnPieceCompleted(Piece piece)
    {
        lock (_lock)
        {
            _pending = true;
            if (DateTime.UtcNow - _lastSave >= _minInterval)
                SaveLocked();
        }
    }

    private void SaveLocked()
    {
        var completed = _dataHandler.CompletedPieces.Select(p => p.Index);
        _store.Save(_directory, new ResumeData(
            _dataHandler.Metainfo.InfoHash,
            _dataHandler.Metainfo.Pieces.Count,
            completed));
        _lastSave = DateTime.UtcNow;
        _pending = false;
    }
}
