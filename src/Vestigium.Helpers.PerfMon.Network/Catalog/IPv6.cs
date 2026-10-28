namespace Vestigium.Helpers.PerfMon.Network;

/// <summary>PDH category IPv6. Generated from EventCatalog/pdh-categories.json.</summary>
public static class IPv6
{
    public const string Category = "IPv6";
    public const string DatagramsForwardedPerSec = "Datagrams Forwarded/sec";
    public const string DatagramsOutboundDiscarded = "Datagrams Outbound Discarded";
    public const string DatagramsOutboundNoRoute = "Datagrams Outbound No Route";
    public const string DatagramsReceivedAddressErrors = "Datagrams Received Address Errors";
    public const string DatagramsReceivedDeliveredPerSec = "Datagrams Received Delivered/sec";
    public const string DatagramsReceivedDiscarded = "Datagrams Received Discarded";
    public const string DatagramsReceivedHeaderErrors = "Datagrams Received Header Errors";
    public const string DatagramsReceivedUnknownProtocol = "Datagrams Received Unknown Protocol";
    public const string DatagramsReceivedPerSec = "Datagrams Received/sec";
    public const string DatagramsSentPerSec = "Datagrams Sent/sec";
    public const string DatagramsPerSec = "Datagrams/sec";
    public const string FragmentReAssemblyFailures = "Fragment Re-assembly Failures";
    public const string FragmentationFailures = "Fragmentation Failures";
    public const string FragmentedDatagramsPerSec = "Fragmented Datagrams/sec";
    public const string FragmentsCreatedPerSec = "Fragments Created/sec";
    public const string FragmentsReAssembledPerSec = "Fragments Re-assembled/sec";
    public const string FragmentsReceivedPerSec = "Fragments Received/sec";

    public static IReadOnlyList<string> Counters { get; } =
    [
        DatagramsForwardedPerSec,
        DatagramsOutboundDiscarded,
        DatagramsOutboundNoRoute,
        DatagramsReceivedAddressErrors,
        DatagramsReceivedDeliveredPerSec,
        DatagramsReceivedDiscarded,
        DatagramsReceivedHeaderErrors,
        DatagramsReceivedUnknownProtocol,
        DatagramsReceivedPerSec,
        DatagramsSentPerSec,
        DatagramsPerSec,
        FragmentReAssemblyFailures,
        FragmentationFailures,
        FragmentedDatagramsPerSec,
        FragmentsCreatedPerSec,
        FragmentsReAssembledPerSec,
        FragmentsReceivedPerSec,
    ];
}
