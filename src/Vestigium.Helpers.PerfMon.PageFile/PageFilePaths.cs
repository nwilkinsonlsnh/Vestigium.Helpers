namespace Vestigium.Helpers.PerfMon.PageFile;

/// <summary>
/// Paging File short job. File expansion is PF01.003.
/// </summary>
internal static class PageFilePaths
{
    public static string ObjectName => PageFileObjects.PagingFile;

    public static readonly string[] ShortCounters =
    [
        "% Usage",
        "% Usage Peak"
    ];

    public static string InstanceOrTotal(string? instance)
        => string.IsNullOrWhiteSpace(instance) ? "_Total" : instance.Trim();

    public static IReadOnlyList<CounterPath> Usage(string? instance = "_Total")
        => For(new PageFileSampleOptions { Instance = instance ?? "_Total" });

    public static IReadOnlyList<CounterPath> For(PageFileSampleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var inst = InstanceOrTotal(options.Instance);
        return ShortCounters
            .Select(name => new CounterPath(ObjectName, name, inst, PageFileCounterCatalog.UnitOf(name)))
            .ToArray();
    }
}
