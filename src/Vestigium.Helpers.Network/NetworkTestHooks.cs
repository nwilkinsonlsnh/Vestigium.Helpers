namespace Vestigium.Helpers.Network;

/// <summary>
/// Test injection only. Hosts must not set these.
/// </summary>
public static class NetworkTestHooks
{
    public static string? CampaignRoot { get; set; }
    public static DateTimeOffset? UtcNow { get; set; }

    internal static DateTimeOffset Now()
        => UtcNow ?? DateTimeOffset.UtcNow;

    internal static void Reset()
    {
        CampaignRoot = null;
        UtcNow = null;
    }
}
