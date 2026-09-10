using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

/// <summary>
/// Workstation inventory and protocol jobs for diagnostic hosts.
/// Logging is <see cref="HelperLog"/> → Vestigium.Logging JSONL (APPID Network).
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
}
