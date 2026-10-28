namespace Vestigium.Helpers.PerfMon;

/// <summary>Test double. Programmed rows and misses. Not a public host API.</summary>
internal sealed class FakeCounterSource : ICounterSource
{
    private readonly Dictionary<string, Queue<SampleRecord>> _reads =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, List<string>> _instances =
        new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _prime =
        new(StringComparer.OrdinalIgnoreCase);

    public void Seed(CounterPath path, params SampleRecord[] rows)
    {
        ArgumentNullException.ThrowIfNull(path);
        ArgumentNullException.ThrowIfNull(rows);
        var queue = Queue(path);
        foreach (var row in rows)
            queue.Enqueue(row);
    }

    public void SeedMiss(CounterPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        Queue(path).Enqueue(SampleRecord.Unavailable(path));
    }

    public void SeedInstances(string category, IEnumerable<string> names)
    {
        ArgumentNullException.ThrowIfNull(names);
        var key = category?.Trim() ?? string.Empty;
        if (key.Length == 0)
            return;
        _instances[key] = names
            .Select(n => n?.Trim() ?? string.Empty)
            .Where(n => n.Length > 0)
            .ToList();
    }

    public void MarkNeedsPrime(CounterPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        _prime.Add(path.Key);
    }

    public bool NeedsPrime(CounterPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        return _prime.Contains(path.Key);
    }

    public SampleRecord Read(CounterPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        if (_reads.TryGetValue(path.Key, out var queue) && queue.Count > 0)
            return queue.Dequeue();
        return SampleRecord.Unavailable(path);
    }

    public IReadOnlyList<string> ListInstances(string category, int cap)
    {
        if (cap <= 0)
            return Array.Empty<string>();

        var key = category?.Trim() ?? string.Empty;
        if (key.Length == 0 || !_instances.TryGetValue(key, out var names) || names.Count == 0)
            return Array.Empty<string>();

        if (names.Count <= cap)
            return names.ToArray();

        return names.Take(cap).ToArray();
    }

    private Queue<SampleRecord> Queue(CounterPath path)
    {
        if (!_reads.TryGetValue(path.Key, out var queue))
        {
            queue = new Queue<SampleRecord>();
            _reads[path.Key] = queue;
        }

        return queue;
    }
}
