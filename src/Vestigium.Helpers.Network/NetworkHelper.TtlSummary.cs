namespace Vestigium.Helpers.Network;

public static partial class NetworkHelper
{
    public static TtlSummaryResult SummarizeTtl(IEnumerable<IcmpEchoReply>? replies)
        => TtlSummary.FromReplies(replies);

    public static TtlSummaryResult SummarizeTtl(IcmpEchoResult? result)
        => TtlSummary.FromResult(result);
}
