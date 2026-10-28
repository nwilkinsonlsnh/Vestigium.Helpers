using System.Diagnostics;
using System.Net.NetworkInformation;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class CounterSampleEngine
{
    public static NetworkJob<CounterSampleResult> Create(string nameOrId, CounterSampleOptions? options)
    {
        var key = HelperGuard.NotBlank(nameOrId, nameof(nameOrId)).Trim();
        var o = options ?? new CounterSampleOptions();
        Guard(o);
        if (Find(key) is null)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Adapter, nameof(Create), "adapter missing");
            throw new ArgumentException("Adapter was not found.", nameof(nameOrId));
        }

        var jobId = "counters-" + HelperLog.NewId();
        return new NetworkJob<CounterSampleResult>(jobId, "counterSample", (token, progress) => RunAsync(jobId, key, o, token, progress));
    }

    internal static void Guard(CounterSampleOptions o)
    {
        if (o.Duration < CounterSampleOptions.MinDuration || o.Duration > CounterSampleOptions.MaxDuration)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Adapter, nameof(Guard), $"Duration={o.Duration}");
            throw new ArgumentOutOfRangeException(nameof(o.Duration), "Duration must be between 10 ms and 1 hour.");
        }

        if (o.Interval is { } interval)
        {
            if (interval <= TimeSpan.Zero || interval > o.Duration)
            {
                HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Adapter, nameof(Guard), $"Interval={interval}");
                throw new ArgumentOutOfRangeException(nameof(o.Interval), "Interval must be greater than 0 and at most Duration.");
            }
        }
    }

    internal static CounterReading Delta(CounterReading start, CounterReading end)
        => new(
            NonNegative(end.BytesIn - start.BytesIn),
            NonNegative(end.BytesOut - start.BytesOut),
            NonNegative(end.ErrorsIn - start.ErrorsIn),
            NonNegative(end.ErrorsOut - start.ErrorsOut),
            NonNegative(end.DiscardsIn - start.DiscardsIn),
            NonNegative(end.DiscardsOut - start.DiscardsOut));

    private static long NonNegative(long value) => value < 0 ? 0 : value;

    private static async Task<CounterSampleResult> RunAsync(
        string jobId,
        string nameOrId,
        CounterSampleOptions options,
        CancellationToken token,
        IProgress<NetworkProgress>? progress)
    {
        var nic = Find(nameOrId) ?? throw new InvalidOperationException("Adapter disappeared.");
        NetworkLog.Pending(HelperLog.Subcategories.Adapter, $"counterSample job={jobId} adapter={nic.Name} durationMs={(int)options.Duration.TotalMilliseconds}");

        var start = Read(nic);
        var samples = new List<CounterSample>();
        if (options.Interval is { } interval)
            samples.Add(new CounterSample(DateTimeOffset.UtcNow, start));

        var clock = Stopwatch.StartNew();
        var next = options.Interval ?? options.Duration;
        while (clock.Elapsed < options.Duration && !token.IsCancellationRequested)
        {
            var remain = options.Duration - clock.Elapsed;
            var wait = remain < next ? remain : next;
            if (wait > TimeSpan.Zero)
            {
                try { await Task.Delay(wait, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }

            if (options.Interval is null)
                break;

            nic = Find(nameOrId) ?? nic;
            samples.Add(new CounterSample(DateTimeOffset.UtcNow, Read(nic)));
            progress?.Report(new NetworkProgress
            {
                JobId = jobId,
                Phase = "Sample",
                Sent = samples.Count,
                LastStatus = nic.Name
            });
        }

        nic = Find(nameOrId) ?? nic;
        var end = Read(nic);
        var delta = Delta(start, end);
        if (options.Interval is not null && (samples.Count == 0 || samples[^1].Reading != end))
            samples.Add(new CounterSample(DateTimeOffset.UtcNow, end));

        var result = new CounterSampleResult(jobId, nic.Name, nic.Id, clock.Elapsed, start, end, delta, samples);
        NetworkLog.Success(
            HelperLog.Subcategories.Adapter,
            $"counterSample job={jobId} adapter={nic.Name} in={delta.BytesIn} out={delta.BytesOut} samples={samples.Count}");
        return result;
    }

    internal static CounterReading Read(NetworkInterface nic)
    {
        try
        {
            var stats = nic.GetIPStatistics();
            return new CounterReading(
                stats.BytesReceived,
                stats.BytesSent,
                stats.IncomingPacketsWithErrors,
                stats.OutgoingPacketsWithErrors,
                stats.IncomingPacketsDiscarded,
                stats.OutgoingPacketsDiscarded);
        }
        catch (NetworkInformationException)
        {
            return new CounterReading(0, 0, 0, 0, 0, 0);
        }
    }

    internal static NetworkInterface? Find(string nameOrId)
    {
        foreach (var nic in NetworkInterface.GetAllNetworkInterfaces())
        {
            if (nic.Id.Equals(nameOrId, StringComparison.OrdinalIgnoreCase)
                || nic.Name.Equals(nameOrId, StringComparison.OrdinalIgnoreCase)
                || nic.Description.Equals(nameOrId, StringComparison.OrdinalIgnoreCase))
                return nic;
        }

        return null;
    }
}
