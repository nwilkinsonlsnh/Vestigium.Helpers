using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

/// <summary>
/// Workstation inventory and protocol jobs for diagnostic hosts.
/// Logging is <see cref="HelperLog"/> → Vestigium.Logging JSONL (APPID Network).
/// Campaign statistics are JSONL via Vestigium.Helpers.Json.
/// </summary>
public static class NetworkHelper
{
    public static string Identity => "Vestigium.Helpers.Network";

    public static string Probe()
    {
        using var scope = NetworkLog.Begin(HelperLog.Subcategories.Probe, nameof(Probe));
        NetworkLog.Pending(HelperLog.Subcategories.Probe, "Describing loopback diagnostic endpoint.");
        NetworkLog.Success(HelperLog.Subcategories.Probe, "Network probe complete. Identity=" + Identity);
        return Identity;
    }

    public static WorkstationNetwork GetWorkstation()
    {
        using var scope = NetworkLog.Begin(HelperLog.Subcategories.Inventory, nameof(GetWorkstation));
        var snapshot = NetworkInventoryEngine.Capture();
        NetworkLog.Success(
            HelperLog.Subcategories.Inventory,
            $"host={snapshot.HostName} adapters={snapshot.Adapters.Count}");
        return snapshot;
    }

    public static IReadOnlyList<NetworkAdapter> GetAdapters(NetworkAdapterQuery? query = null)
    {
        using var scope = NetworkLog.Begin(HelperLog.Subcategories.Adapter, nameof(GetAdapters));
        var snapshot = NetworkInventoryEngine.Capture(query);
        NetworkLog.Success(HelperLog.Subcategories.Adapter, $"adapters={snapshot.Adapters.Count}");
        return snapshot.Adapters;
    }

    public static NetworkAdapter GetAdapter(string nameOrId)
    {
        using var scope = NetworkLog.Begin(HelperLog.Subcategories.Adapter, nameof(GetAdapter), nameOrId);
        var adapter = NetworkInventoryEngine.CaptureOne(nameOrId);
        NetworkLog.Success(
            HelperLog.Subcategories.Adapter,
            $"name={adapter.Name} addresses={adapter.UnicastAddresses.Count}");
        return adapter;
    }

    public static NetworkJob<IcmpEchoResult> IcmpEcho(string target, IcmpEchoOptions? options = null)
        => IcmpEchoEngine.Create(target, options);

    public static NetworkJob<IcmpEchoResult> Ping(string target, IcmpEchoOptions? options = null)
        => IcmpEcho(target, options);

    public static NetworkJob<IcmpTraceResult> IcmpTrace(string target, IcmpTraceOptions? options = null)
        => IcmpTraceEngine.Create(target, options);

    public static NetworkJob<IcmpTraceResult> Trace(string target, IcmpTraceOptions? options = null)
        => IcmpTrace(target, options);

    public static Task<DnsLookupResult> LookupAsync(
        string name,
        DnsLookupOptions? options = null,
        CancellationToken cancellation = default)
        => DnsClient.LookupAsync(name, options, cancellation);

    public static Task<IReadOnlyList<DnsLookupResult>> LookupManyAsync(
        IEnumerable<string> names,
        DnsLookupOptions? options = null,
        CancellationToken cancellation = default)
        => DnsClient.LookupManyAsync(names, options, cancellation);

    public static IReadOnlyList<NetworkConnection> GetConnections(NetworkConnectionQuery? query = null)
    {
        using var scope = NetworkLog.Begin(HelperLog.Subcategories.Connection, nameof(GetConnections));
        var rows = NetworkStackEngine.GetConnections(query);
        NetworkLog.Success(HelperLog.Subcategories.Connection, $"connections={rows.Count}");
        return rows;
    }

    public static NetworkStackStatistics GetStatistics()
    {
        using var scope = NetworkLog.Begin(HelperLog.Subcategories.Connection, nameof(GetStatistics));
        var stats = NetworkStackEngine.GetStatistics();
        NetworkLog.Success(HelperLog.Subcategories.Connection, "statistics captured");
        return stats;
    }

    public static IReadOnlyList<NetworkRoute> GetRoutes(RouteFamily family = RouteFamily.All)
    {
        using var scope = NetworkLog.Begin(HelperLog.Subcategories.Route, nameof(GetRoutes));
        var rows = NetworkStackEngine.GetRoutes(family);
        NetworkLog.Success(HelperLog.Subcategories.Route, $"routes={rows.Count} family={family}");
        return rows;
    }

    public static IReadOnlyList<NetworkNeighbor> GetNeighbors()
    {
        using var scope = NetworkLog.Begin(HelperLog.Subcategories.Neighbor, nameof(GetNeighbors));
        var rows = NetworkStackEngine.GetNeighbors();
        NetworkLog.Success(HelperLog.Subcategories.Neighbor, $"neighbors={rows.Count}");
        return rows;
    }

    public static IcmpEchoCampaign CreateEchoCampaign(IcmpEchoCampaignOptions options)
        => IcmpEchoCampaign.Create(options);

    public static IcmpEchoCampaign OpenEchoCampaign(string recipePath)
        => IcmpEchoCampaign.Open(recipePath);
}
