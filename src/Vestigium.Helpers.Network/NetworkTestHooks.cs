namespace Vestigium.Helpers.Network;

/// <summary>
/// Test injection only. Not part of the public surface.
/// Visible to <c>Vestigium.Helpers.Tests</c> via InternalsVisibleTo.
/// </summary>
internal static class NetworkTestHooks
{
    internal static string? CampaignRoot { get; set; }
    internal static string? ShareRoot { get; set; }
    internal static DateTimeOffset? UtcNow { get; set; }
    internal static string? ProcRoot { get; set; }
    internal static IReadOnlyList<double>? ProbeBytesPerSecond { get; set; }
    internal static IReadOnlyDictionary<string, IReadOnlyList<double>>? ProbeRatesByWorkload { get; set; }

    internal static DateTimeOffset Now()
        => UtcNow ?? DateTimeOffset.UtcNow;

    internal static string ProcPath(string linuxPath)
    {
        if (string.IsNullOrWhiteSpace(ProcRoot))
            return linuxPath;
        var relative = linuxPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
        return Path.Combine(ProcRoot, relative);
    }

    internal static void Reset()
    {
        CampaignRoot = null;
        ShareRoot = null;
        UtcNow = null;
        ProcRoot = null;
        ProbeBytesPerSecond = null;
        ProbeRatesByWorkload = null;
    }
}
