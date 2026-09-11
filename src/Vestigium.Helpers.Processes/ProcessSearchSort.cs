namespace Vestigium.Helpers.Processes;

internal static class ProcessSearchSort
{
    public static IReadOnlyList<ProcessInfo> TakeStable(IEnumerable<ProcessInfo> hits, int maxResults)
        => hits
            .OrderBy(row => row.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(row => row.Pid)
            .Take(maxResults)
            .ToArray();
}
