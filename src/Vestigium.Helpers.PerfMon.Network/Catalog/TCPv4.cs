namespace Vestigium.Helpers.PerfMon.Network;

/// <summary>PDH category TCPv4. Generated from EventCatalog/pdh-categories.json.</summary>
public static class TCPv4
{
    public const string Category = "TCPv4";
    public const string ConnectionFailures = "Connection Failures";
    public const string ConnectionsActive = "Connections Active";
    public const string ConnectionsEstablished = "Connections Established";
    public const string ConnectionsPassive = "Connections Passive";
    public const string ConnectionsReset = "Connections Reset";
    public const string SegmentsReceivedPerSec = "Segments Received/sec";
    public const string SegmentsRetransmittedPerSec = "Segments Retransmitted/sec";
    public const string SegmentsSentPerSec = "Segments Sent/sec";
    public const string SegmentsPerSec = "Segments/sec";

    public static IReadOnlyList<string> Counters { get; } =
    [
        ConnectionFailures,
        ConnectionsActive,
        ConnectionsEstablished,
        ConnectionsPassive,
        ConnectionsReset,
        SegmentsReceivedPerSec,
        SegmentsRetransmittedPerSec,
        SegmentsSentPerSec,
        SegmentsPerSec,
    ];
}
