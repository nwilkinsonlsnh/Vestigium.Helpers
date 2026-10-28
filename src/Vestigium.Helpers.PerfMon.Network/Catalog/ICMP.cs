namespace Vestigium.Helpers.PerfMon.Network;

/// <summary>PDH category ICMP. Generated from EventCatalog/pdh-categories.json.</summary>
public static class ICMP
{
    public const string Category = "ICMP";
    public const string MessagesOutboundErrors = "Messages Outbound Errors";
    public const string MessagesReceivedErrors = "Messages Received Errors";
    public const string MessagesReceivedPerSec = "Messages Received/sec";
    public const string MessagesSentPerSec = "Messages Sent/sec";
    public const string MessagesPerSec = "Messages/sec";
    public const string ReceivedAddressMask = "Received Address Mask";
    public const string ReceivedAddressMaskReply = "Received Address Mask Reply";
    public const string ReceivedDestUnreachable = "Received Dest. Unreachable";
    public const string ReceivedEchoReplyPerSec = "Received Echo Reply/sec";
    public const string ReceivedEchoPerSec = "Received Echo/sec";
    public const string ReceivedParameterProblem = "Received Parameter Problem";
    public const string ReceivedRedirectPerSec = "Received Redirect/sec";
    public const string ReceivedSourceQuench = "Received Source Quench";
    public const string ReceivedTimeExceeded = "Received Time Exceeded";
    public const string ReceivedTimestampReplyPerSec = "Received Timestamp Reply/sec";
    public const string ReceivedTimestampPerSec = "Received Timestamp/sec";
    public const string SentAddressMask = "Sent Address Mask";
    public const string SentAddressMaskReply = "Sent Address Mask Reply";
    public const string SentDestinationUnreachable = "Sent Destination Unreachable";
    public const string SentEchoReplyPerSec = "Sent Echo Reply/sec";
    public const string SentEchoPerSec = "Sent Echo/sec";
    public const string SentParameterProblem = "Sent Parameter Problem";
    public const string SentRedirectPerSec = "Sent Redirect/sec";
    public const string SentSourceQuench = "Sent Source Quench";
    public const string SentTimeExceeded = "Sent Time Exceeded";
    public const string SentTimestampReplyPerSec = "Sent Timestamp Reply/sec";
    public const string SentTimestampPerSec = "Sent Timestamp/sec";

    public static IReadOnlyList<string> Counters { get; } =
    [
        MessagesOutboundErrors,
        MessagesReceivedErrors,
        MessagesReceivedPerSec,
        MessagesSentPerSec,
        MessagesPerSec,
        ReceivedAddressMask,
        ReceivedAddressMaskReply,
        ReceivedDestUnreachable,
        ReceivedEchoReplyPerSec,
        ReceivedEchoPerSec,
        ReceivedParameterProblem,
        ReceivedRedirectPerSec,
        ReceivedSourceQuench,
        ReceivedTimeExceeded,
        ReceivedTimestampReplyPerSec,
        ReceivedTimestampPerSec,
        SentAddressMask,
        SentAddressMaskReply,
        SentDestinationUnreachable,
        SentEchoReplyPerSec,
        SentEchoPerSec,
        SentParameterProblem,
        SentRedirectPerSec,
        SentSourceQuench,
        SentTimeExceeded,
        SentTimestampReplyPerSec,
        SentTimestampPerSec,
    ];
}
