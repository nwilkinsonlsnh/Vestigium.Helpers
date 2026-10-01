using System.Diagnostics;

namespace Vestigium.Helpers.PerfMon;

/// <summary>
/// The one public local PDH source. Keeps <see cref="PerformanceCounter"/> alive across reads
/// so rate counters can return a rate. Hosts do not subclass this type.
/// Missing category, instance, or access denied is Unavailable.
/// The first read of a rate counter is Unavailable. It is the prime, not a sample. Never 0.
/// <see cref="Retain"/> drops counters the current job does not sample. A new NIC instance does not keep the old handle.
/// Not a remote collector.
/// </summary>
public sealed class CachedPdhSource : ICounterSource, IDisposable
{
    private readonly Dictionary<string, PerformanceCounter> _live = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _rates = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _primed = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();
    private bool _disposed;

    public int OpenedCount
    {
        get { lock (_gate) return _live.Count; }
    }

    public SampleRecord Read(CounterPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var key = Key(path);
            try
            {
                if (!TryOpen(path, key, out var counter))
                    return Miss(path, key);

                if (_rates.Contains(key) && _primed.Add(key))
                {
                    _ = counter.NextValue();
                    return SampleRecord.Unavailable(path);
                }

                _primed.Add(key);
                return SampleRecord.Ok(path, counter.NextValue());
            }
            catch (InvalidOperationException)
            {
                return Miss(path, key);
            }
            catch (ArgumentException)
            {
                return Miss(path, key);
            }
            catch (UnauthorizedAccessException)
            {
                return Miss(path, key);
            }
            catch (Exception ex)
            {
                Drop(key);
                PerfMonLog.Error(
                    PerfMonEvents.SourceThrown,
                    Vestigium.Logging.VestigiumStatus.Failed,
                    PerfMonCatalog.Subcategories.Source,
                    "unexpected PDH failure",
                    ex);
                throw;
            }
        }
    }

    /// <summary>
    /// Dispose counters whose path is not in <paramref name="keep"/>.
    /// SampleJob calls this at the start of a run so an adapter switch does not leak the previous instance.
    /// </summary>
    public void Retain(IReadOnlyList<CounterPath> keep)
    {
        ArgumentNullException.ThrowIfNull(keep);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var wanted = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var path in keep)
            {
                if (path is null)
                    continue;
                wanted.Add(Key(path));
            }

            foreach (var key in _live.Keys.Where(key => !wanted.Contains(key)).ToArray())
                Drop(key);
        }
    }

    public IReadOnlyList<string> ListInstances(string category, int cap)
    {
        if (cap <= 0 || string.IsNullOrWhiteSpace(category))
            return [];

        try
        {
            var cat = new PerformanceCounterCategory(category.Trim());
            if (cat.CategoryType == PerformanceCounterCategoryType.SingleInstance)
                return [];
            var names = cat.GetInstanceNames();
            return names.Length <= cap ? names : names.Take(cap).ToArray();
        }
        catch (InvalidOperationException)
        {
            return [];
        }
        catch (ArgumentException)
        {
            return [];
        }
    }

    public bool NeedsPrime(CounterPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var key = Key(path);
            if (_primed.Contains(key))
                return false;
            if (!_rates.Contains(key) && !TryOpen(path, key, out _))
                return false;
            return _rates.Contains(key);
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            if (_disposed)
                return;
            _disposed = true;
            foreach (var counter in _live.Values)
            {
                try { counter.Dispose(); }
                catch (Exception) { }
            }

            _live.Clear();
            _rates.Clear();
            _primed.Clear();
        }
    }

    private bool TryOpen(CounterPath path, string key, out PerformanceCounter counter)
    {
        if (_live.TryGetValue(key, out counter!))
            return true;

        counter = Open(path);
        _live[key] = counter;
        if (IsRate(counter.CounterType))
            _rates.Add(key);
        return true;
    }

    private SampleRecord Miss(CounterPath path, string key)
    {
        Drop(key);
        PerfMonLog.Warning(
            PerfMonEvents.SourceUnavailable,
            Vestigium.Logging.VestigiumStatus.Failed,
            PerfMonCatalog.Subcategories.Source,
            "category or instance missing",
            properties: PerfMonLog.Props(("category", path.Category), ("counter", path.Counter), ("instance", path.Instance)));
        return SampleRecord.Unavailable(path);
    }

    private void Drop(string key)
    {
        if (_live.Remove(key, out var counter))
        {
            try { counter.Dispose(); }
            catch (Exception) { }
        }

        _rates.Remove(key);
        _primed.Remove(key);
    }

    private static string Key(CounterPath path) => $"{path.Category}\u001f{path.Counter}\u001f{path.Instance}";

    private static PerformanceCounter Open(CounterPath path)
        => path.Instance.Length == 0
            ? new PerformanceCounter(path.Category, path.Counter, readOnly: true)
            : new PerformanceCounter(path.Category, path.Counter, path.Instance, readOnly: true);

    private static bool IsRate(PerformanceCounterType type)
        => type is
            PerformanceCounterType.RateOfCountsPerSecond32 or
            PerformanceCounterType.RateOfCountsPerSecond64 or
            PerformanceCounterType.CountPerTimeInterval32 or
            PerformanceCounterType.CountPerTimeInterval64 or
            PerformanceCounterType.CounterTimer or
            PerformanceCounterType.CounterTimerInverse or
            PerformanceCounterType.Timer100Ns or
            PerformanceCounterType.Timer100NsInverse or
            PerformanceCounterType.ElapsedTime or
            PerformanceCounterType.SampleCounter or
            PerformanceCounterType.SampleFraction or
            PerformanceCounterType.CounterMultiTimer or
            PerformanceCounterType.CounterMultiTimerInverse or
            PerformanceCounterType.CounterMultiTimer100Ns or
            PerformanceCounterType.CounterMultiTimer100NsInverse or
            PerformanceCounterType.AverageTimer32 or
            PerformanceCounterType.RawFraction;
}
