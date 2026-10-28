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
        var table = NetworkNetBiosFallback.Stats();
        var hasTable = table.RegisteredByBroadcast > 0 || table.ResolvedByBroadcast > 0 || table.RegisteredByNameServer > 0 || table.ResolvedByNameServer > 0;
        if (!hasTable)
            return stats;
        var node = stats.NodeType;
        if (string.IsNullOrWhiteSpace(node) || node.StartsWith("LANA ", StringComparison.Ordinal) || node.StartsWith("enum ", StringComparison.Ordinal))
            node = table.NodeType;
        return table with { NodeType = node };
    }
}
