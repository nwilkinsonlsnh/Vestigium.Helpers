namespace Vestigium.Helpers.PerfMon.Network;

/// <summary>PDH category ICMPv6. Generated from EventCatalog/pdh-categories.json.</summary>
public static class ICMPv6
{
    public const string Category = "ICMPv6";
    public const string MessagesOutboundErrors = "Messages Outbound Errors";
    public const string MessagesReceivedErrors = "Messages Received Errors";
    public const string MessagesReceivedPerSec = "Messages Received/sec";
    public const string MessagesSentPerSec = "Messages Sent/sec";
    public const string MessagesPerSec = "Messages/sec";
    public const string ReceivedDestUnreachable = "Received Dest. Unreachable";
    public const string ReceivedEchoReplyPerSec = "Received Echo Reply/sec";
    public const string ReceivedEchoPerSec = "Received Echo/sec";
    public const string ReceivedMembershipQuery = "Received Membership Query";
    public const string ReceivedMembershipReduction = "Received Membership Reduction";
    public const string ReceivedMembershipReport = "Received Membership Report";
    public const string ReceivedNeighborAdvert = "Received Neighbor Advert";
    public const string ReceivedNeighborSolicit = "Received Neighbor Solicit";
    public const string ReceivedPacketTooBig = "Received Packet Too Big";
    public const string ReceivedParameterProblem = "Received Parameter Problem";
    public const string ReceivedRedirectPerSec = "Received Redirect/sec";
    public const string ReceivedRouterAdvert = "Received Router Advert";
    public const string ReceivedRouterSolicit = "Received Router Solicit";
    public const string ReceivedTimeExceeded = "Received Time Exceeded";
    public const string SentDestinationUnreachable = "Sent Destination Unreachable";
    public const string SentEchoReplyPerSec = "Sent Echo Reply/sec";
    public const string SentEchoPerSec = "Sent Echo/sec";
    public const string SentMembershipQuery = "Sent Membership Query";
    public const string SentMembershipReduction = "Sent Membership Reduction";
    public const string SentMembershipReport = "Sent Membership Report";
    public const string SentNeighborAdvert = "Sent Neighbor Advert";
    public const string SentNeighborSolicit = "Sent Neighbor Solicit";
    public const string SentPacketTooBig = "Sent Packet Too Big";
    public const string SentParameterProblem = "Sent Parameter Problem";
    public const string SentRedirectPerSec = "Sent Redirect/sec";
    public const string SentRouterAdvert = "Sent Router Advert";
    public const string SentRouterSolicit = "Sent Router Solicit";
    public const string SentTimeExceeded = "Sent Time Exceeded";

    public static IReadOnlyList<string> Counters { get; } =
    [
        MessagesOutboundErrors,
        MessagesReceivedErrors,
        MessagesReceivedPerSec,
        MessagesSentPerSec,
        MessagesPerSec,
        ReceivedDestUnreachable,
        ReceivedEchoReplyPerSec,
        ReceivedEchoPerSec,
        ReceivedMembershipQuery,
        ReceivedMembershipReduction,
        ReceivedMembershipReport,
        ReceivedNeighborAdvert,
        ReceivedNeighborSolicit,
        ReceivedPacketTooBig,
        ReceivedParameterProblem,
        ReceivedRedirectPerSec,
        ReceivedRouterAdvert,
        ReceivedRouterSolicit,
        ReceivedTimeExceeded,
        SentDestinationUnreachable,
        SentEchoReplyPerSec,
        SentEchoPerSec,
        SentMembershipQuery,
        SentMembershipReduction,
        SentMembershipReport,
        SentNeighborAdvert,
        SentNeighborSolicit,
        SentPacketTooBig,
        SentParameterProblem,
        SentRedirectPerSec,
        SentRouterAdvert,
        SentRouterSolicit,
        SentTimeExceeded,
    ];
}
