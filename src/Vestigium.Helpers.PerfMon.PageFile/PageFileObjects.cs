namespace Vestigium.Helpers.PerfMon.PageFile;

/// <summary>PDH objects this probe is allowed to name.</summary>
public static class PageFileObjects
{
    public const string PagingFile = "Paging File";

    public static IReadOnlyList<string> All { get; } = [PagingFile];
}
