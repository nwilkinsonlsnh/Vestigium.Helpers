namespace Vestigium.Helpers.PerfMon.Memory;

/// <summary>
/// Short job on the Memory object. Never emits Paging File.
/// Empty instance stays empty. No invented _Total.
/// </summary>
internal static class MemoryPaths
{
    public const string PageFileObject = "Paging File";
    public static string ObjectName => MemoryObjects.Memory;

    public static readonly string[] ShortCounters =
    [
        Memory.AvailableMBytes,
        Memory.CommittedBytes,
        Memory.CommitLimit,
        Memory.PercentCommittedBytesInUse,
        Memory.CacheBytes,
        Memory.PagesPerSec
    ];

    public static string InstanceOrNone(string? instance)
        => string.IsNullOrWhiteSpace(instance) ? string.Empty : instance.Trim();

    public static IReadOnlyList<CounterPath> Short(string? instance = null)
        => For(new MemorySampleOptions { Instance = instance ?? "" });

    public static IReadOnlyList<CounterPath> For(MemorySampleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var inst = InstanceOrNone(options.Instance);
        return ShortCounters
            .Select(name => new CounterPath(MemoryObjects.Memory, name, inst, MemoryCounterCatalog.UnitOf(name)))
            .ToArray();
    }
}
