namespace Vestigium.PerfMon.Network;

/// <summary>
/// Identity door for Vestigium.PerfMon.Network. Hosts call <see cref="Register"/> inside
/// <c>VestigiumLogger.Initialize</c>. This library never initializes the host.
/// </summary>
public static class NetworkPerfCatalog
{
    /// <summary>Suggested APPID when this library is the process.</summary>
    public const string AppId = "PerfMon.Network";

    /// <summary>Catalog category.</summary>
    public const string Category = "PerfMon";

    /// <summary>
    /// Reserved for host registration. Skeleton does not register events yet.
    /// </summary>
    public static void Register(object cfg)
    {
        ArgumentNullException.ThrowIfNull(cfg);
    }
}
