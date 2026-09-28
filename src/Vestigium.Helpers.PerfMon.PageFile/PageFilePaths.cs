namespace Vestigium.Helpers.PerfMon.PageFile;

/// <summary>
/// Paging File short job. Files are listed once at job start.
/// </summary>
internal static class PageFilePaths
{
    public static string ObjectName => PagingFile.Category;

    public static readonly string[] ShortCounters =
    [
        PagingFile.PercentUsage,
        PagingFile.PercentUsagePeak
    ];

    public static string InstanceOrTotal(string? instance)
        => string.IsNullOrWhiteSpace(instance) ? "_Total" : instance.Trim();

    public static IReadOnlyList<string> Files(ICounterInventory? inventory, int cap)
    {
        if (cap <= 0)
            return Array.Empty<string>();

        var take = cap > int.MaxValue - 1 ? cap : cap + 1;
        return PageFileCounterCatalog
            .LiveInstances(ObjectName, take, inventory)
            .Where(name => !name.Equals("_Total", StringComparison.OrdinalIgnoreCase))
            .Take(cap)
            .ToArray();
    }

    public static IReadOnlyList<CounterPath> Usage(string? instance = "_Total")
        => For(new PageFileSampleOptions { Instance = instance ?? "_Total" });

    public static IReadOnlyList<CounterPath> For(PageFileSampleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var inst = InstanceOrTotal(options.Instance);
        var rows = new List<CounterPath>();
        AddInstance(rows, inst);

        if (options.IncludeFiles)
        {
            foreach (var file in Files(options.Inventory, options.InstanceCap))
            {
                if (file.Equals(inst, StringComparison.OrdinalIgnoreCase))
                    continue;
                AddInstance(rows, file);
            }
        }

        return rows;
    }

    private static void AddInstance(List<CounterPath> rows, string instance)
    {
        foreach (var name in ShortCounters)
            rows.Add(new CounterPath(ObjectName, name, instance, PageFileCounterCatalog.UnitOf(name)));
    }
}
