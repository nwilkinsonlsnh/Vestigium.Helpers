namespace Vestigium.Helpers.PerfMon.Network;

/// <summary>PDH category Network Adapter. Generated from EventCatalog/pdh-categories.json.</summary>
public static class NetworkAdapter
{
    public const string Category = "Network Adapter";
    public const string BytesReceivedPerSec = "Bytes Received/sec";
    public const string BytesSentPerSec = "Bytes Sent/sec";
    public const string BytesTotalPerSec = "Bytes Total/sec";
    public const string CurrentBandwidth = "Current Bandwidth";
    public const string OffloadedConnections = "Offloaded Connections";
    public const string OutputQueueLength = "Output Queue Length";
    public const string PacketsOutboundDiscarded = "Packets Outbound Discarded";
    public const string PacketsOutboundErrors = "Packets Outbound Errors";
    public const string PacketsReceivedDiscarded = "Packets Received Discarded";
    public const string PacketsReceivedErrors = "Packets Received Errors";
    public const string PacketsReceivedNonUnicastPerSec = "Packets Received Non-Unicast/sec";
    public const string PacketsReceivedUnicastPerSec = "Packets Received Unicast/sec";
    public const string PacketsReceivedUnknown = "Packets Received Unknown";
    public const string PacketsReceivedPerSec = "Packets Received/sec";
    public const string PacketsSentNonUnicastPerSec = "Packets Sent Non-Unicast/sec";
    public const string PacketsSentUnicastPerSec = "Packets Sent Unicast/sec";
    public const string PacketsSentPerSec = "Packets Sent/sec";
    public const string PacketsPerSec = "Packets/sec";
    public const string TCPActiveRSCConnections = "TCP Active RSC Connections";
    public const string TCPRSCAveragePacketSize = "TCP RSC Average Packet Size";
    public const string TCPRSCCoalescedPacketsPerSec = "TCP RSC Coalesced Packets/sec";
    public const string TCPRSCExceptionsPerSec = "TCP RSC Exceptions/sec";

    public static IReadOnlyList<string> Counters { get; } =
    [
        BytesReceivedPerSec,
        BytesSentPerSec,
        BytesTotalPerSec,
        CurrentBandwidth,
        OffloadedConnections,
        OutputQueueLength,
        PacketsOutboundDiscarded,
        PacketsOutboundErrors,
        PacketsReceivedDiscarded,
        PacketsReceivedErrors,
        PacketsReceivedNonUnicastPerSec,
        PacketsReceivedUnicastPerSec,
        PacketsReceivedUnknown,
        PacketsReceivedPerSec,
        PacketsSentNonUnicastPerSec,
        PacketsSentUnicastPerSec,
        PacketsSentPerSec,
        PacketsPerSec,
        TCPActiveRSCConnections,
        TCPRSCAveragePacketSize,
        TCPRSCCoalescedPacketsPerSec,
        TCPRSCExceptionsPerSec,
    ];
}
