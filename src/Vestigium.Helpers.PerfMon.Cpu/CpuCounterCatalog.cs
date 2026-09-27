namespace Vestigium.Helpers.PerfMon.Cpu;

/// <summary>
/// Known vocabulary plus live checks and a watch for Processor,
/// Processor Information, and Processor Performance.
/// Live methods never substitute the known list.
/// </summary>
public static class CpuCounterCatalog
{
    public const int DefaultCap = 256;

    internal static readonly ICpuInventory Pdh = new PdhCpuInventory();

    private static readonly Dictionary<string, string[]> Known = new(StringComparer.OrdinalIgnoreCase)
    {
        [CpuObjects.Processor] =
        [
            "% Processor Time",
            "% User Time",
            "% Privileged Time",
            "% Interrupt Time",
            "% DPC Time",
            "% Idle Time",
            "Interrupts/sec",
            "DPCs Queued/sec",
            "DPC Rate",
            "C1 Transitions/sec",
            "C2 Transitions/sec",
            "C3 Transitions/sec",
            "% C1 Time",
            "% C2 Time",
            "% C3 Time"
        ],
        [CpuObjects.ProcessorInformation] =
        [
            "% Processor Time",
            "% User Time",
            "% Privileged Time",
            "% Interrupt Time",
            "% DPC Time",
            "% Idle Time",
            "% Priority Time",
            "Interrupts/sec",
            "DPCs Queued/sec",
            "DPC Rate",
            "Parking Status",
            "Processor Frequency",
            "% of Maximum Frequency",
            "Processor State Flags",
            "Idle Break Events/sec",
            "Clock Interrupts/sec",
            "Average Idle Time",
            "C1 Transitions/sec",
            "C2 Transitions/sec",
            "C3 Transitions/sec",
            "% C1 Time",
            "% C2 Time",
            "% C3 Time",
            "% Performance Limit",
            "Performance Limit Flags"
        ],
        [CpuObjects.ProcessorPerformance] =
        [
            "Processor Frequency",
            "% of Maximum Frequency",
            "% of Maximum Performance",
            "Processor Performance",
            "Processor Utility",
            "Privileged Utility",
            "% Performance Limit",
            "Performance Limit Flags",
            "Processor RTC"
        ]
    };

    public static IReadOnlyList<string> Categories => CpuObjects.All;

    public static bool IsKnownCategory(string category)
        => Known.ContainsKey(category?.Trim() ?? string.Empty);

    public static bool IsKnownCounter(string category, string counter)
    {
        if (!IsKnownCategory(category) || string.IsNullOrWhiteSpace(counter))
            return false;
        return Counters(category).Contains(counter.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public static IReadOnlyList<string> Counters(string category)
        => Known[RequireCategory(category)];

    public static bool CategoryPresent(string category, ICpuInventory? inventory = null)
        => inventory.OrPdh().CategoryPresent(RequireCategory(category));

    public static bool HasCounter(string category, string counter, string instance = "_Total", ICpuInventory? inventory = null)
    {
        if (string.IsNullOrWhiteSpace(counter))
            return false;
        var snap = Snapshot(category, instance, DefaultCap, inventory);
        return snap.CategoryPresent
            && snap.Counters.Contains(counter.Trim(), StringComparer.OrdinalIgnoreCase);
    }

    public static bool HasInstance(string category, string instance, ICpuInventory? inventory = null)
    {
        var key = RequireCategory(category);
        var inv = inventory.OrPdh();
        if (!inv.CategoryPresent(key))
            return false;
        if (string.IsNullOrWhiteSpace(instance))
            return true;
        return inv.InstancePresent(key, instance.Trim());
    }

    public static IReadOnlyList<string> LiveCounters(string category, string instance = "_Total", int cap = DefaultCap, ICpuInventory? inventory = null)
        => inventory.OrPdh().LiveCounters(RequireCategory(category), instance, cap);

    public static IReadOnlyList<string> LiveInstances(string category, int cap = DefaultCap, ICpuInventory? inventory = null)
        => inventory.OrPdh().LiveInstances(RequireCategory(category), cap);

    public static CpuCatalogSnapshot Snapshot(
        string category,
        string instance = "_Total",
        int cap = DefaultCap,
        ICpuInventory? inventory = null,
        TimeProvider? clock = null)
    {
        var key = RequireCategory(category);
        var inst = string.IsNullOrWhiteSpace(instance) ? "_Total" : instance.Trim();
        var inv = inventory.OrPdh();
        var present = inv.CategoryPresent(key);
        return new CpuCatalogSnapshot(
            (clock ?? TimeProvider.System).GetUtcNow(),
            key,
            inst,
            present,
            present ? inv.LiveCounters(key, inst, cap) : Array.Empty<string>(),
            present ? inv.LiveInstances(key, cap) : Array.Empty<string>());
    }

    public static async Task WatchAsync(
        CpuCatalogWatchOptions options,
        Action<CpuCatalogSnapshot> onSnapshot,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(onSnapshot);
        ArgumentNullException.ThrowIfNull(options.Clock);

        var key = RequireCategory(options.Category);
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
            var sig = Signature(snap);
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

    public static IReadOnlyList<CounterPath> Paths(
        string category,
        string instance = "_Total",
        IEnumerable<string>? counters = null)
    {
        var key = RequireCategory(category);
        var inst = string.IsNullOrWhiteSpace(instance) ? "_Total" : instance.Trim();
        var names = counters is null
            ? Counters(key)
            : counters.Select(n => n?.Trim() ?? string.Empty).Where(n => n.Length > 0).ToArray();
        return names.Select(name => new CounterPath(key, name, inst, UnitOf(name))).ToArray();
    }

    public static string UnitOf(string counter)
    {
        var name = counter?.Trim() ?? string.Empty;
        if (name.StartsWith('%') || name.Contains("%", StringComparison.Ordinal))
            return "%";
        if (name.Contains("/sec", StringComparison.OrdinalIgnoreCase))
            return "/sec";
        if (name.Contains("Frequency", StringComparison.OrdinalIgnoreCase))
            return "MHz";
        if (name.Equals("Parking Status", StringComparison.OrdinalIgnoreCase)
            || name.Contains("Flags", StringComparison.OrdinalIgnoreCase))
            return "flag";
        return string.Empty;
    }

    private static string RequireCategory(string? category)
    {
        var key = category?.Trim() ?? string.Empty;
        if (key.Length == 0 || !Known.ContainsKey(key))
            throw new ArgumentException(
                "Category must be Processor, Processor Information, or Processor Performance.",
                nameof(category));
        return Known.Keys.First(k => k.Equals(key, StringComparison.OrdinalIgnoreCase));
    }

    private static string Signature(CpuCatalogSnapshot snap)
        => $"{snap.CategoryPresent}|{string.Join('\u001f', snap.Counters)}|{string.Join('\u001f', snap.Instances)}";
}

internal static class CpuInventoryExtensions
{
    public static ICpuInventory OrPdh(this ICpuInventory? inventory)
        => inventory ?? CpuCounterCatalog.Pdh;
}
