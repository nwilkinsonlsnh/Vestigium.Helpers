namespace Vestigium.Helpers.PerfMon.Network;

/// <summary>PDH category UDPv4. Generated from EventCatalog/pdh-categories.json.</summary>
public static class UDPv4
{
    public const string Category = "UDPv4";
    public const string DatagramsNoPortPerSec = "Datagrams No Port/sec";
    public const string DatagramsReceivedErrors = "Datagrams Received Errors";
    public const string DatagramsReceivedPerSec = "Datagrams Received/sec";
    public const string DatagramsSentPerSec = "Datagrams Sent/sec";
    public const string DatagramsPerSec = "Datagrams/sec";

    public static IReadOnlyList<string> Counters { get; } =
    [
        DatagramsNoPortPerSec,
        DatagramsReceivedErrors,
        DatagramsReceivedPerSec,
        DatagramsSentPerSec,
        DatagramsPerSec,
    ];
}
