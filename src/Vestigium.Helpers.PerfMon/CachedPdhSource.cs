using System.Diagnostics;

namespace Vestigium.Helpers.PerfMon;

/// <summary>
/// The one public local PDH source. Keeps <see cref="PerformanceCounter"/> alive across reads
/// so rate counters can return a rate. Hosts do not subclass this type.
/// Missing category, instance, or access denied is Unavailable. Not a remote collector.
/// SampleJob still constructs <c>PerformanceCounterSource</c> until PR03c.002.
/// </summary>
public sealed class CachedPdhSource : ICounterSource, IDisposable
{
    private readonly Dictionary<string, PerformanceCounter> _live = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _primed = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _gate = new();
    private bool _disposed;

    public SampleRecord Read(CounterPath path)
    {
        ArgumentNullException.ThrowIfNull(path);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            var key = Key(path);
            try
            {
                if (!_live.TryGetValue(key, out var counter))
                {
                    counter = Open(path);
                    _live[key] = counter;
                }

                var value = counter.NextValue();
                _primed.Add(key);
                return SampleRecord.Ok(path, value);
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
            return !_primed.Contains(Key(path));
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
            _primed.Clear();
        }
    }

    private SampleRecord Miss(CounterPath path, string key)
    {
        _primed.Add(key);
        PerfMonLog.Warning(
            PerfMonEvents.SourceUnavailable,
            Vestigium.Logging.VestigiumStatus.Failed,
            PerfMonCatalog.Subcategories.Source,
            "category or instance missing",
            properties: PerfMonLog.Props(("category", path.Category), ("counter", path.Counter), ("instance", path.Instance)));
        return SampleRecord.Unavailable(path);
    }

    private static string Key(CounterPath path) => $"{path.Category}\u001f{path.Counter}\u001f{path.Instance}";

    private static PerformanceCounter Open(CounterPath path)
        => path.Instance.Length == 0
            ? new PerformanceCounter(path.Category, path.Counter, readOnly: true)
            : new PerformanceCounter(path.Category, path.Counter, path.Instance, readOnly: true);
}
