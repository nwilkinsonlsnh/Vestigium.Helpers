namespace Vestigium.Helpers.PerfMon.Cpu;

/// <summary>One live view of an object. Frozen lists.</summary>
public sealed class CpuCatalogSnapshot
{
    public CpuCatalogSnapshot(
        DateTimeOffset utc,
        string category,
        string instance,
        bool categoryPresent,
        IReadOnlyList<string> counters,
        IReadOnlyList<string> instances)
    {
        Utc = utc;
        Category = category;
        Instance = instance;
        CategoryPresent = categoryPresent;
        Counters = counters.ToArray();
        Instances = instances.ToArray();
    }

    public DateTimeOffset Utc { get; }
    public string Category { get; }
    public string Instance { get; }
    public bool CategoryPresent { get; }
    public IReadOnlyList<string> Counters { get; }
    public IReadOnlyList<string> Instances { get; }
}
