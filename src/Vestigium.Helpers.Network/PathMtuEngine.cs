using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class PathMtuEngine
{
    public static NetworkJob<PathMtuResult> Create(string target, PathMtuOptions? options)
    {
        var host = HelperGuard.NotBlank(target, nameof(target)).Trim();
        var o = options ?? new PathMtuOptions();
        Guard(o);
        EgressBind.Validate(o.InterfaceIndex, o.SourceAddress);
        var jobId = "pmtu-" + HelperLog.NewId();
        return new NetworkJob<PathMtuResult>(jobId, "pathMtu", (token, progress) => RunAsync(jobId, host, o, token, progress));
    }

    internal static void Guard(PathMtuOptions o)
    {
        if (o.MinPayload < IcmpEchoOptions.MinBufferSize || o.MinPayload > IcmpEchoOptions.MaxBufferSize)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), $"MinPayload={o.MinPayload}");
            throw new ArgumentOutOfRangeException(nameof(o.MinPayload), "MinPayload must be between 1 and 65500.");
        }

        if (o.MaxPayload < o.MinPayload || o.MaxPayload > IcmpEchoOptions.MaxBufferSize)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), $"MaxPayload={o.MaxPayload}");
            throw new ArgumentOutOfRangeException(nameof(o.MaxPayload), "MaxPayload must be between MinPayload and 65500.");
        }

        var timeoutMs = o.Timeout.TotalMilliseconds;
        if (timeoutMs is < IcmpEchoOptions.MinTimeoutMs or > IcmpEchoOptions.MaxTimeoutMs)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), $"TimeoutMs={timeoutMs:0}");
            throw new ArgumentOutOfRangeException(nameof(o.Timeout), "Timeout must be between 10 ms and 60 s.");
        }
    }

    internal static PathMtuOutcome Classify(IcmpEchoStatus status, string? detail)
    {
        if (status == IcmpEchoStatus.Success)
            return PathMtuOutcome.Passed;
        if (status == IcmpEchoStatus.DestinationUnreachable)
            return PathMtuOutcome.TooBig;
        if (detail is not null
            && (detail.Contains("PacketTooBig", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("NeedFrag", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("MessageTooLong", StringComparison.OrdinalIgnoreCase)
                || detail.Contains("too big", StringComparison.OrdinalIgnoreCase)))
            return PathMtuOutcome.TooBig;
        return PathMtuOutcome.Unknown;
    }

    internal static void Step(PathMtuOutcome outcome, int mid, ref int lo, ref int hi, ref int? largest, HashSet<int> unknown)
    {
        switch (outcome)
        {
            case PathMtuOutcome.Passed:
                largest = mid;
                lo = mid + 1;
                break;
            case PathMtuOutcome.TooBig:
                hi = mid - 1;
                break;
            default:
                unknown.Add(mid);
                break;
        }
    }

    internal static int? NextSize(int lo, int hi, HashSet<int> unknown)
    {
        if (lo > hi)
            return null;
        var mid = lo + ((hi - lo) / 2);
        if (!unknown.Contains(mid))
            return mid;
        for (var size = lo; size <= hi; size++)
        {
            if (!unknown.Contains(size))
                return size;
        }

        return null;
    }

    private static async Task<PathMtuResult> RunAsync(
        string jobId,
        string target,
        PathMtuOptions options,
        CancellationToken token,
        IProgress<NetworkProgress>? progress)
    {
        NetworkLog.Pending(HelperLog.Subcategories.Icmp, $"pathMtu job={jobId} target={target} min={options.MinPayload} max={options.MaxPayload}");
        var tries = new List<PathMtuTry>();
        int? largest = null;
        string? resolved = null;
        var lo = options.MinPayload;
        var hi = options.MaxPayload;
        var unknown = new HashSet<int>();

        while (!token.IsCancellationRequested)
        {
            var mid = NextSize(lo, hi, unknown);
            if (mid is null)
                break;

            var row = await ProbeAsync(target, mid.Value, options, token).ConfigureAwait(false);
            tries.Add(row.Try);
            resolved ??= row.Resolved;
            progress?.Report(new NetworkProgress
            {
                JobId = jobId,
                Phase = "Pmtu",
                Sent = tries.Count,
                Received = tries.Count(t => t.Passed),
                Sequence = mid.Value,
                LastStatus = row.Try.Outcome.ToString(),
                LastRoundtripMs = row.Try.RoundtripTimeMs
            });

            Step(row.Try.Outcome, mid.Value, ref lo, ref hi, ref largest, unknown);
        }

        var hitCeiling = largest == options.MaxPayload;
        var status = token.IsCancellationRequested
            ? NetworkJobStatus.Cancelled
            : largest is null ? NetworkJobStatus.Failed : NetworkJobStatus.Success;
        IcmpEchoEngine.LogFinished(status, $"{status} pathMtu job={jobId} target={target} largest={largest?.ToString() ?? "none"} tries={tries.Count}");
        return new PathMtuResult(jobId, target, resolved, status, largest, hitCeiling, tries);
    }

    private static async Task<(PathMtuTry Try, string? Resolved)> ProbeAsync(
        string target,
        int payload,
        PathMtuOptions options,
        CancellationToken token)
    {
        var job = IcmpEchoEngine.Create(target, new IcmpEchoOptions
        {
            Count = 1,
            Timeout = options.Timeout,
            Interval = TimeSpan.Zero,
            BufferSize = payload,
            DontFragment = true,
            InterfaceIndex = options.InterfaceIndex,
            SourceAddress = options.SourceAddress
        });
        var result = await job.RunAsync(token).ConfigureAwait(false);
        var reply = result.Replies.FirstOrDefault();
        var status = reply?.Status ?? IcmpEchoStatus.Failed;
        var outcome = Classify(status, reply?.Detail);
        var rtt = reply?.RoundtripTimeMs ?? 0;
        return (new PathMtuTry(payload, outcome == PathMtuOutcome.Passed, outcome, status, rtt), result.ResolvedAddress);
    }
}
