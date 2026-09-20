using Vestigium.Helpers.FileIo;

namespace Vestigium.Helpers.Network;

internal static class ShareCampaignEngine
{
    public static ShareCampaignResult RunDefault(string campaignId, ShareCampaignOptions options, CancellationToken cancellation)
    {
        using var scope = NetworkLog.Begin(HelperLog.Subcategories.Share, nameof(RunDefault), campaignId);
        var planned = options.PlannedSize ?? throw new InvalidOperationException("Default mode requires PlannedSize.");
        var resultsPath = ResolveResults(campaignId, options);
        var samples = CollectDefaultRates(options, cancellation);
        var bits = samples.Select(b => (decimal)(b * 8d)).ToArray();
        var bill = PercentileBillEngine.FromSamples(bits, 0.95);
        var transfer = BandwidthEngine.TransferTime(planned, bill.Rate);
        var measured = Scale(transfer.Duration, options.Efficiency);
        TimeSpan? declared = options.DeclaredPipeRate is { } pipe
            ? BandwidthEngine.TransferTime(planned, pipe).Duration
            : null;

        WriteJsonl(resultsPath, campaignId, "Default", measured, payload: measured, metadata: TimeSpan.Zero);
        NetworkLog.Success(HelperLog.Subcategories.Share, $"default campaign={campaignId} duration={measured}");
        return new ShareCampaignResult(
            campaignId, NetworkJobStatus.Success, ShareCampaignMode.Default, resultsPath,
            ShareCampaign.DefaultDisclaimer, measured, bill.Rate, declared, measured, TimeSpan.Zero);
    }

    public static ShareCampaignResult RunAdvanced(string campaignId, ShareCampaignOptions options, CancellationToken cancellation)
    {
        using var scope = NetworkLog.Begin(HelperLog.Subcategories.Share, nameof(RunAdvanced), campaignId);
        var analysis = options.SourceAnalysis ?? throw new InvalidOperationException("Advanced mode requires SourceAnalysis.");
        var plan = ShareProbePlanner.Plan(analysis, new ShareProbeOptions
        {
            MaxProbeBytes = options.MaxProbeBytes,
            IncludeReadProbe = options.IncludeReadProbe,
            PlannedBytesOverride = options.PlannedSize is { } size ? (long)(size.Bits / 8m) : null
        });

        var rates = new Dictionary<string, List<double>>(StringComparer.OrdinalIgnoreCase);
        foreach (var step in plan.Probes)
        {
            cancellation.ThrowIfCancellationRequested();
            var key = step.IsMetadata ? "ManySmall" : step.Workload;
            if (!rates.TryGetValue(key, out var list))
            {
                list = [];
                rates[key] = list;
            }

            list.AddRange(CollectStepRates(options, step, cancellation));
        }

        var fallback = P95BytesPerSecond(rates.Where(kv => !kv.Key.Equals("ManySmall", StringComparison.OrdinalIgnoreCase)).SelectMany(kv => kv.Value));
        if (fallback <= 0)
            throw new InvalidOperationException("Advanced estimate needs at least one payload probe rate.");

        double payloadSeconds = 0;
        if (analysis.Buckets is not null)
        {
            foreach (var bucket in analysis.Buckets)
            {
                if (bucket.TotalBytes <= 0)
                    continue;
                var bps = rates.TryGetValue(bucket.Name, out var samples) && samples.Count > 0
                    ? P95BytesPerSecond(samples)
                    : fallback;
                payloadSeconds += bucket.TotalBytes / bps / options.Efficiency;
            }
        }

        var payload = TimeSpan.FromSeconds(payloadSeconds);
        var metadata = TimeSpan.Zero;
        if (plan.Probes.Any(p => p.IsMetadata) && rates.TryGetValue("ManySmall", out var metaRates) && metaRates.Count > 0)
        {
            var bps = P95BytesPerSecond(metaRates);
            var secondsPerFile = ShareProbePlanner.MetadataFileBytes / bps;
            var tinySmall = analysis.Buckets.Where(b => b.Id is FileIoBucket.Tiny or FileIoBucket.Small).Sum(b => b.FileCount);
            metadata = TimeSpan.FromSeconds(secondsPerFile * tinySmall);
        }

        var measured = payload + metadata;
        var planned = options.PlannedSize ?? BandwidthEngine.From(analysis.TotalBytes, DataUnit.Byte);
        TimeSpan? declared = options.DeclaredPipeRate is { } pipe
            ? BandwidthEngine.TransferTime(planned, pipe).Duration
            : null;
        var resultsPath = ResolveResults(campaignId, options);
        var disclaimer =
            $"payload {payload.TotalHours:0.###} h + metadata {metadata.TotalHours:0.###} h. Declared-pipe estimate is separate.";

        WriteJsonl(resultsPath, campaignId, "Advanced", measured, payload, metadata);
        NetworkLog.Success(HelperLog.Subcategories.Share, $"advanced campaign={campaignId} {disclaimer}");
        return new ShareCampaignResult(
            campaignId, NetworkJobStatus.Success, ShareCampaignMode.Advanced, resultsPath,
            disclaimer, measured, BandwidthEngine.From((decimal)(fallback * 8d), DataUnit.Bit), declared, payload, metadata);
    }

    static IReadOnlyList<double> CollectDefaultRates(ShareCampaignOptions options, CancellationToken cancellation)
    {
        if (NetworkTestHooks.ProbeBytesPerSecond is { Count: > 0 } injected)
            return injected;
        return WriteProbes(options.Target.Directory, options.ProbeBytes, options.ProbeCount, options.IncludeReadProbe, cancellation);
    }

    static IReadOnlyList<double> CollectStepRates(ShareCampaignOptions options, ShareProbeStep step, CancellationToken cancellation)
    {
        var key = step.IsMetadata ? "ManySmall" : step.Workload;
        if (NetworkTestHooks.ProbeRatesByWorkload is { } map && map.TryGetValue(key, out var injected) && injected.Count > 0)
            return injected;
        if (NetworkTestHooks.ProbeBytesPerSecond is { Count: > 0 } all)
            return all;

        return WriteProbes(options.Target.Directory, step.ProbeBytes, step.ProbeCount, options.IncludeReadProbe, cancellation);
    }

    static List<double> WriteProbes(string directory, long bytes, int count, bool includeRead, CancellationToken cancellation)
    {
        var size = FileIoSize.FromBytes(bytes);
        var rates = new List<double>(count);
        for (var i = 0; i < count; i++)
        {
            cancellation.ThrowIfCancellationRequested();
            var written = FileIoHelper.WriteProbe(directory, size);
            rates.Add(written.BytesPerSecond);
            if (includeRead && !written.Deleted)
                rates.Add(FileIoHelper.ReadProbe(written.Path).BytesPerSecond);
        }

        return rates;
    }

    static double P95BytesPerSecond(IEnumerable<double> samples)
    {
        var list = samples.Where(v => v > 0 && double.IsFinite(v)).Select(v => (decimal)(v * 8d)).ToArray();
        if (list.Length == 0)
            return 0;
        var bill = PercentileBillEngine.FromSamples(list, 0.95);
        return (double)(bill.Rate.Bits / 8m);
    }

    static TimeSpan Scale(TimeSpan duration, double efficiency)
        => TimeSpan.FromTicks((long)Math.Round(duration.Ticks / efficiency, MidpointRounding.AwayFromZero));

    static void WriteJsonl(string resultsPath, string campaignId, string mode, TimeSpan measured, TimeSpan payload, TimeSpan metadata)
    {
        CampaignJsonl.AppendCampaign(resultsPath, new { kind = "campaignStart", campaignId, mode, recordedUtc = NetworkTestHooks.Now() });
        CampaignJsonl.AppendCampaign(resultsPath, new { kind = "windowSummary", campaignId, payloadSeconds = payload.TotalSeconds, metadataSeconds = metadata.TotalSeconds, recordedUtc = NetworkTestHooks.Now() });
        CampaignJsonl.AppendCampaign(resultsPath, new { kind = "campaignEnd", campaignId, measuredSeconds = measured.TotalSeconds, recordedUtc = NetworkTestHooks.Now() });
    }

    static string ResolveResults(string campaignId, ShareCampaignOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.ResultsPath))
            return CampaignPaths.Confine(options.ResultsPath, nameof(options.ResultsPath));
        var root = CampaignPaths.Root();
        Directory.CreateDirectory(root);
        return Path.Combine(root, campaignId + ".jsonl");
    }
}
