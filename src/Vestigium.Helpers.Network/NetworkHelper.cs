using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

/// <summary>
/// Workstation inventory and protocol jobs for diagnostic hosts.
/// Logging is <see cref="HelperLog"/> → Vestigium.Logging JSONL (APPID Network).
/// Reachability is ICMP Echo. Path is ICMP TTL-walk. Names are RFC 1035.
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
}
