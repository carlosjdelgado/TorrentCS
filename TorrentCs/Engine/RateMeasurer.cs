namespace TorrentCs.Engine;

public class RateMeasurer
{
    private const int WindowSeconds = 30;

    private readonly LinkedList<RateMeasurement> _measurements = new();
    private readonly object _lock = new();

    public void AddMeasure(long value)
    {
        lock (_lock)
        {
            _measurements.AddLast(new RateMeasurement(DateTime.UtcNow, value));
            Clean();
        }
    }

    public void Reset()
    {
        lock (_lock)
            _measurements.Clear();
    }

    public long AverageRate()
    {
        lock (_lock)
        {
            Clean();
            if (_measurements.Count < 2) return 0;

            long total = _measurements.Sum(m => m.Value);
            return total * 1000 / (WindowSeconds * 1000);
        }
    }

    private void Clean()
    {
        var cutoff = DateTime.UtcNow.AddSeconds(-WindowSeconds);
        while (_measurements.First is { } node && node.Value.Timestamp < cutoff)
            _measurements.RemoveFirst();
    }

    private sealed class RateMeasurement(DateTime timestamp, long value)
    {
        public DateTime Timestamp { get; } = timestamp;
        public long Value { get; } = value;
    }
}
