namespace Vestigium.Helpers.Network;

public static class TtlSummary
{
    private static readonly int[] InitialTtls = [64, 128, 255];

    public static TtlSummaryResult FromReplies(IEnumerable<IcmpEchoReply>? replies)
    {
        var ttls = (replies ?? [])
            .Where(static reply => reply.Status == IcmpEchoStatus.Success && reply.Ttl > 0)
            .Select(static reply => reply.Ttl)
            .ToArray();
        return FromValues(ttls);
    }

    public static TtlSummaryResult FromResult(IcmpEchoResult? result)
        => FromReplies(result?.Replies);

    private static TtlSummaryResult FromValues(int[] ttls)
    {
        if (ttls.Length == 0)
            return new TtlSummaryResult(0, null, null, null, null, null);

        var min = ttls.Min();
        var max = ttls.Max();
        var mode = Mode(ttls);
        var (estimated, initial) = Estimate(mode);
        return new TtlSummaryResult(ttls.Length, min, max, mode, estimated, initial);
    }

    private static int Mode(int[] ttls)
    {
        return ttls
            .GroupBy(static ttl => ttl)
            .OrderByDescending(static group => group.Count())
            .ThenBy(static group => group.Key)
            .First()
            .Key;
    }

    private static (int Estimated, int Initial) Estimate(int observed)
    {
        var initial = InitialTtls.First(value => value >= observed);
        return (initial - observed, initial);
    }
}
