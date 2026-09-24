using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class PathpingEngine
{
    public static NetworkJob<PathpingResult> Create(string target, PathpingOptions? options)
    {
        var host = HelperGuard.NotBlank(target, nameof(target)).Trim();
        var o = options ?? new PathpingOptions();
        Guard(o);
        EgressBind.Validate(o.InterfaceIndex, o.SourceAddress);
        TraceResolve.Guard(o.Family);
        TraceResolve.GuardLiteral(host, o.Family);
        var jobId = "pathping-" + HelperLog.NewId();
        return new NetworkJob<PathpingResult>(jobId, "pathping", (token, progress) => RunAsync(jobId, host, o, token, progress));
    }

    private static void Guard(PathpingOptions o)
    {
        if (o.MaxHops is < 1 or > 64)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), $"MaxHops={o.MaxHops}");
            throw new ArgumentOutOfRangeException(nameof(o.MaxHops), "MaxHops must be between 1 and 64.");
        }

        if (o.ProbesPerHop is < 1 or > 10)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), $"ProbesPerHop={o.ProbesPerHop}");
            throw new ArgumentOutOfRangeException(nameof(o.ProbesPerHop), "ProbesPerHop must be between 1 and 10.");
        }

        if (o.SamplesPerHop < PathpingOptions.MinSamplesPerHop || o.SamplesPerHop > PathpingOptions.MaxSamplesPerHop)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), $"SamplesPerHop={o.SamplesPerHop}");
            throw new ArgumentOutOfRangeException(nameof(o.SamplesPerHop), "SamplesPerHop must be between 1 and 100.");
        }

        var timeoutMs = o.Timeout.TotalMilliseconds;
        if (timeoutMs is < IcmpEchoOptions.MinTimeoutMs or > IcmpEchoOptions.MaxTimeoutMs)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), $"TimeoutMs={timeoutMs:0}");
            throw new ArgumentOutOfRangeException(nameof(o.Timeout), "Timeout must be between 10 ms and 60 s.");
        }

        if (o.SampleInterval < TimeSpan.Zero)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Icmp, nameof(Guard), "SampleInterval is negative");
            throw new ArgumentOutOfRangeException(nameof(o.SampleInterval), "SampleInterval cannot be negative.");
        }
    }

    private static async Task<PathpingResult> RunAsync(
        string jobId,
        string target,
        PathpingOptions options,
        CancellationToken token,
        IProgress<NetworkProgress>? progress)
    {
        NetworkLog.Pending(HelperLog.Subcategories.Icmp, $"pathping job={jobId} target={target} samples={options.SamplesPerHop}");

        var walkJob = IcmpTraceEngine.Create(target, ToTrace(options));
        var walk = await walkJob.RunAsync(token).ConfigureAwait(false);
        var protocol = SettledProtocol(walk);
        progress?.Report(new NetworkProgress
        {
            JobId = jobId,
            Phase = "Walk",
            Sent = walk.HopCount,
            Received = walk.Hops.Count(h => h.Address is not null),
            LastStatus = protocol.ToString()
        });

        var sampled = new List<PathpingHop>(walk.Hops.Count);
        for (var i = 0; i < walk.Hops.Count && !token.IsCancellationRequested; i++)
        {
            var hop = walk.Hops[i];
            sampled.Add(await SampleHopAsync(hop, protocol, options, token).ConfigureAwait(false));
            progress?.Report(new NetworkProgress
            {
                JobId = jobId,
                Phase = "Sample",
                Sent = i + 1,
                Received = sampled.Count(h => h.Received > 0),
                Sequence = hop.Ttl,
                LastStatus = hop.Address ?? "*"
            });
        }

        var hops = ApplyLinkLoss(sampled);
        var status = DecideStatus(token.IsCancellationRequested, walk, hops);
        var line = $"{status} pathping job={jobId} target={target} hops={hops.Count} reached={walk.Reached} sample={protocol}";
        IcmpEchoEngine.LogFinished(status, line);
        return new PathpingResult(jobId, target, walk.ResolvedAddress, status, walk.Reached, protocol, walk, hops);
    }

    internal static ProbeProtocol SettledProtocol(IcmpTraceResult walk)
        => walk.ProbeProtocol;

    private static async Task<PathpingHop> SampleHopAsync(
        IcmpTraceHop hop,
        ProbeProtocol protocol,
        PathpingOptions options,
        CancellationToken token)
    {
        if (string.IsNullOrWhiteSpace(hop.Address))
            return new PathpingHop(hop.Ttl, null, 0, 0, 0, 0, 0, null, null, null, hop.Name);

        if (protocol == ProbeProtocol.Icmp)
            return await SampleIcmpAsync(hop, options, token).ConfigureAwait(false);

        var timeoutMs = Math.Clamp((int)options.Timeout.TotalMilliseconds, IcmpEchoOptions.MinTimeoutMs, IcmpEchoOptions.MaxTimeoutMs);
        var times = new List<long>();
        var received = 0;
        for (var i = 1; i <= options.SamplesPerHop && !token.IsCancellationRequested; i++)
        {
            IcmpTraceProbe row;
            if (protocol == ProbeProtocol.Tcp)
            {
                row = await IcmpTraceEngine.TcpProbeAsync(
                    hop.Address, timeoutMs, 64, i, token, options.Family, options.TcpPort, options.InterfaceIndex, options.SourceAddress)
                    .ConfigureAwait(false);
            }
            else
            {
                row = await IcmpTraceEngine.UdpProbeAsync(hop.Address, timeoutMs, 64, i, token, options.Family).ConfigureAwait(false);
            }

            if (SampleHit(row))
            {
                received++;
                times.Add(row.RoundtripTimeMs);
            }

            if (options.SampleInterval > TimeSpan.Zero && i < options.SamplesPerHop)
            {
                try { await Task.Delay(options.SampleInterval, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
        }

        var samples = options.SamplesPerHop;
        var lost = Math.Max(0, samples - received);
        var hopLoss = samples == 0 ? 0 : 100.0 * lost / samples;
        return new PathpingHop(
            hop.Ttl,
            hop.Address,
            samples,
            received,
            lost,
            hopLoss,
            0,
            times.Count == 0 ? null : times.Min(),
            times.Count == 0 ? null : times.Max(),
            times.Count == 0 ? null : times.Average(),
            hop.Name);
    }

    internal static bool SampleHit(IcmpTraceProbe row)
        => row.Status is IcmpEchoStatus.Success or IcmpEchoStatus.TtlExpired
           || (row.Protocol == ProbeProtocol.Tcp && row.Address is not null);

    private static async Task<PathpingHop> SampleIcmpAsync(IcmpTraceHop hop, PathpingOptions options, CancellationToken token)
    {
        var echo = IcmpEchoEngine.Create(hop.Address!, new IcmpEchoOptions
        {
            Count = options.SamplesPerHop,
            Timeout = options.Timeout,
            Interval = options.SampleInterval,
            BufferSize = Math.Max(IcmpEchoOptions.MinBufferSize, options.BufferSize),
            InterfaceIndex = options.InterfaceIndex,
            SourceAddress = options.SourceAddress
        });
        var result = await echo.RunAsync(token).ConfigureAwait(false);
        var samples = result.Sent;
        var received = result.Received;
        var lost = Math.Max(0, samples - received);
        var hopLoss = samples == 0 ? 0 : 100.0 * lost / samples;
        return new PathpingHop(
            hop.Ttl,
            hop.Address,
            samples,
            received,
            lost,
            hopLoss,
            0,
            result.MinMs,
            result.MaxMs,
            result.AverageMs,
            hop.Name);
    }

    internal static IReadOnlyList<PathpingHop> ApplyLinkLoss(IReadOnlyList<PathpingHop> hops)
    {
        var rows = new PathpingHop[hops.Count];
        for (var i = 0; i < hops.Count; i++)
        {
            var hop = hops[i];
            var link = 0d;
            if (i + 1 < hops.Count)
                link = LinkLossPercent(hop.HopLossPercent, hops[i + 1].HopLossPercent);
            rows[i] = hop with { LinkLossPercent = link };
        }

        return rows;
    }

    internal static double LinkLossPercent(double thisHopLossPercent, double nextHopLossPercent)
        => Math.Max(0, nextHopLossPercent - thisHopLossPercent);

    private static IcmpTraceOptions ToTrace(PathpingOptions o)
        => new()
        {
            MaxHops = o.MaxHops,
            ProbesPerHop = o.ProbesPerHop,
            Timeout = o.Timeout,
            BufferSize = o.BufferSize,
            PreferUdp = o.PreferUdp,
            TcpPort = o.TcpPort,
            InterfaceIndex = o.InterfaceIndex,
            SourceAddress = o.SourceAddress,
            Family = o.Family
        };

    private static NetworkJobStatus DecideStatus(bool cancelled, IcmpTraceResult walk, IReadOnlyList<PathpingHop> hops)
    {
        if (cancelled)
            return NetworkJobStatus.Cancelled;
        if (walk.Reached || hops.Any(h => h.Received > 0))
            return NetworkJobStatus.Success;
        return walk.Status;
    }
}
