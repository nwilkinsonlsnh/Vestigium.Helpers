namespace Vestigium.Helpers.PerfMon.Network;

/// <summary>
/// Network Interface short job. Never emits Helpers.Network types.
/// Adapter expansion is PN01.003.
/// </summary>
internal static class NetworkPaths
{
    public static string ObjectName => NetworkObjects.NetworkInterface;

    public static readonly string[] InterfaceShort =
    [
        "Bytes Total/sec",
        "Bytes Received/sec",
        "Bytes Sent/sec",
        "Packets/sec",
        "Packets Received Errors",
        "Packets Outbound Errors",
        "Output Queue Length"
    ];

    public static string InstanceOrTotal(string? instance)
        => string.IsNullOrWhiteSpace(instance) ? "_Total" : instance.Trim();

    public static IReadOnlyList<CounterPath> Interface(string? instance = "_Total")
        => For(new NetworkSampleOptions { Instance = instance ?? "_Total" });

    public static IReadOnlyList<CounterPath> For(NetworkSampleOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        var inst = InstanceOrTotal(options.Instance);
        return InterfaceShort
            .Select(name => new CounterPath(ObjectName, name, inst, NetworkCounterCatalog.UnitOf(name)))
            .ToArray();
    }
}
