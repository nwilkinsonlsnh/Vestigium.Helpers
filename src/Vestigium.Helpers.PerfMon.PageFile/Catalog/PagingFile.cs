namespace Vestigium.Helpers.PerfMon.PageFile;

/// <summary>PDH category Paging File. Generated from EventCatalog/pdh-categories.json.</summary>
public static class PagingFile
{
    public const string Category = "Paging File";
    public const string PercentUsage = "% Usage";
    public const string PercentUsagePeak = "% Usage Peak";

    public static IReadOnlyList<string> Counters { get; } =
    [
        PercentUsage,
        PercentUsagePeak
    ];
}
