namespace Vestigium.Helpers.Network;

public static partial class NetworkHelper
{
    public static IReadOnlyList<NetworkNetBiosName> GetNetBiosNames()
    {
        var rows = NetworkNetBios.Read();
        return rows.Count > 0 ? rows : NetworkNetBiosFallback.Read();
    }

    public static NetworkNetBiosStats GetNetBiosStats()
    {
        var stats = NetworkNetBios.ReadStats();
        if (stats.ResolvedByBroadcast == 0 && stats.ResolvedByNameServer == 0 && stats.RegisteredByBroadcast == 0 && stats.RegisteredByNameServer == 0)
        {
            var table = NetworkNetBiosFallback.Stats();
            if (table.RegisteredByBroadcast > 0 || table.ResolvedByBroadcast > 0 || table.RegisteredByNameServer > 0 || table.ResolvedByNameServer > 0)
                return table with { NodeType = stats.NodeType ?? table.NodeType };
        }

        return stats;
    }
}
