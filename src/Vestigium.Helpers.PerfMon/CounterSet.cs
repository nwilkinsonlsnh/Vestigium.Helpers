namespace Vestigium.Helpers.PerfMon;

/// <summary>
/// Known vocabulary for a probe plus live checks and a watch.
/// Live methods never substitute the known list.
/// </summary>
public sealed class CounterSet
{
    public const int DefaultCap = 256;

    private readonly Dictionary<string, string[]> _known;
    private readonly string _reject;
    private readonly Func<string, string> _unitOf;
    private readonly ICounterInventory _pdh;

    public CounterSet(
        IReadOnlyDictionary<string, string[]> known,
        string rejectMessage,
        Func<string, string>? unitOf = null,
        ICounterInventory? inventory = null)
    {
        ArgumentNullException.ThrowIfNull(known);
        if (known.Count == 0)
            throw new ArgumentException("Known set cannot be empty.", nameof(known));

        _known = new Dictionary<string, string[]>(known, StringComparer.OrdinalIgnoreCase);
        _reject = string.IsNullOrWhiteSpace(rejectMessage)
            ? "Category is not in this set."
            : rejectMessage.Trim();
        _unitOf = unitOf ?? (_ => string.Empty);
        _pdh = inventory ?? PdhCounterInventory.Shared;
        Categories = _known.Keys.ToArray();
    }

    public IReadOnlyList<string> Categories { get; }

    public bool IsKnownCategory(string category)
        => _known.ContainsKey(category?.Trim() ?? string.Empty);

    public bool IsKnownCounter(string category, string counter)
    {
        if (!IsKnownCategory(category) || string.IsNullOrWhiteSpace(counter))
            return false;
        return KnownCounters(category).Contains(counter.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public IReadOnlyList<string> KnownCounters(string category)
        => _known[Require(category)];

    public bool CategoryPresent(string category, ICounterInventory? inventory = null)
        => Inv(inventory).CategoryPresent(Require(category));

    public bool HasCounter(string category, string counter, string instance = "_Total", ICounterInventory? inventory = null)
    {
        if (string.IsNullOrWhiteSpace(counter))
            return false;
        var snap = Snapshot(category, instance, DefaultCap, inventory);
        return snap.CategoryPresent
            && snap.Counters.Contains(counter.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public bool HasInstance(string category, string instance, ICounterInventory? inventory = null)
    {
        var key = Require(category);
        var inv = Inv(inventory);
        if (!inv.CategoryPresent(key))
            return false;
        if (string.IsNullOrWhiteSpace(instance))
            return true;
        return inv.InstancePresent(key, instance.Trim());
    }

    public IReadOnlyList<string> LiveCounters(string category, string instance = "_Total", int cap = DefaultCap, ICounterInventory? inventory = null)
        => Inv(inventory).LiveCounters(Require(category), instance, cap);

    public IReadOnlyList<string> LiveInstances(string category, int cap = DefaultCap, ICounterInventory? inventory = null)
        => Inv(inventory).LiveInstances(Require(category), cap);

    public CatalogSnapshot Snapshot(
        string category,
        string instance = "_Total",
        int cap = DefaultCap,
        ICounterInventory? inventory = null,
        TimeProvider? clock = null)
    {
        var key = Require(category);
        var inst = string.IsNullOrWhiteSpace(instance) ? "_Total" : instance.Trim();
        var inv = Inv(inventory);
        var present = inv.CategoryPresent(key);
        return new CatalogSnapshot(
            (clock ?? TimeProvider.System).GetUtcNow(),
            key,
            inst,
            present,
            present ? inv.LiveCounters(key, inst, cap) : Array.Empty<string>(),
            present ? inv.LiveInstances(key, cap) : Array.Empty<string>());
    }

    public async Task WatchAsync(
        CatalogWatchOptions options,
        Action<CatalogSnapshot> onSnapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(onSnapshot);
        ArgumentNullException.ThrowIfNull(options.Clock);

        var key = Require(options.Category);
        if (options.Interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(options), "Interval must be positive.");
        if (options.Interval < TimeSpan.FromMilliseconds(50))
            throw new ArgumentOutOfRangeException(nameof(options), "Burst floor is 50 ms.");
        if (options.Interval < TimeSpan.FromMilliseconds(200) && !options.AllowBurst)
            throw new ArgumentOutOfRangeException(nameof(options), "Intervals under 200 ms require AllowBurst.");
        if (options.Duration is { } duration && duration > TimeSpan.FromHours(24))
            throw new ArgumentOutOfRangeException(nameof(options), "Duration cap is 24 hours.");
        if (options.Count is < 0)
            throw new ArgumentOutOfRangeException(nameof(options), "Count cannot be negative.");
        if ((options.Count is null or 0) && options.Duration is null && !cancellationToken.CanBeCanceled)
            throw new ArgumentException("Watch must have a count, a duration, or a cancellation token.");

        var started = options.Clock.GetUtcNow();
        string? lastKey = null;
        var ticks = 0;

        while (true)
        {
            if (cancellationToken.IsCancellationRequested)
                return;
            if (options.Count is > 0 && ticks >= options.Count)
                return;
            if (options.Duration is { } cap && options.Clock.GetUtcNow() - started >= cap)
                return;

            var snap = Snapshot(key, options.Instance, options.Cap, options.Inventory, options.Clock);
            var sig = $"{snap.CategoryPresent}|{string.Join('\u001f', snap.Counters)}|{string.Join('\u001f', snap.Instances)}";
            if (!options.EmitOnlyOnChange || sig != lastKey)
            {
                onSnapshot(snap);
                lastKey = sig;
            }

            ticks++;
            if (options.Count is > 0 && ticks >= options.Count)
                return;
            if (options.Duration is { } cap2 && options.Clock.GetUtcNow() - started >= cap2)
                return;

            try
            {
                await Task.Delay(options.Interval, options.Clock, cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                return;
            }
        }
    }

    public IReadOnlyList<CounterPath> Paths(string category, string instance = "_Total", IEnumerable<string>? counters = null)
    {
        var key = Require(category);
        var inst = string.IsNullOrWhiteSpace(instance) ? "_Total" : instance.Trim();
        var names = counters is null
            ? KnownCounters(key)
            : counters.Select(n => n?.Trim() ?? string.Empty).Where(n => n.Length > 0).ToArray();
        return names.Select(name => new CounterPath(key, name, inst, _unitOf(name))).ToArray();
    }

    private ICounterInventory Inv(ICounterInventory? inventory) => inventory ?? _pdh;

    private string Require(string? category)
    {
        var key = category?.Trim() ?? string.Empty;
        if (key.Length == 0 || !_known.ContainsKey(key))
            throw new ArgumentException(_reject, nameof(category));
        return _known.Keys.First(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));
    }
}
