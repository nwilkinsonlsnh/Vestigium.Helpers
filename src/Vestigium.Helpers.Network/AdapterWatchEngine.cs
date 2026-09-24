using System.Diagnostics;
using System.Net.NetworkInformation;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class AdapterWatchEngine
{
    public static NetworkJob<AdapterWatchResult> Create(string nameOrId, AdapterWatchOptions? options)
    {
        var key = HelperGuard.NotBlank(nameOrId, nameof(nameOrId)).Trim();
        var o = options ?? new AdapterWatchOptions();
        Guard(o);
        if (CounterSampleEngine.Find(key) is null)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Adapter, nameof(Create), "adapter missing");
            throw new ArgumentException("Adapter was not found.", nameof(nameOrId));
        }

        var jobId = "watch-" + HelperLog.NewId();
        return new NetworkJob<AdapterWatchResult>(jobId, "watchAdapter", (token, progress) => RunAsync(jobId, key, o, token, progress));
    }

    internal static void Guard(AdapterWatchOptions o)
    {
        if (o.Duration < AdapterWatchOptions.MinDuration || o.Duration > AdapterWatchOptions.MaxDuration)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Adapter, nameof(Guard), $"Duration={o.Duration}");
            throw new ArgumentOutOfRangeException(nameof(o.Duration), "Duration must be between 10 ms and 1 hour.");
        }

        if (o.Interval is { } interval && (interval <= TimeSpan.Zero || interval > o.Duration))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Adapter, nameof(Guard), $"Interval={interval}");
            throw new ArgumentOutOfRangeException(nameof(o.Interval), "Interval must be greater than 0 and at most Duration.");
        }
    }

    internal static AdapterWatchSample Read(NetworkInterface nic)
        => new(DateTimeOffset.UtcNow, nic.OperationalStatus, Speed(nic));

    private static long Speed(NetworkInterface nic)
    {
        try { return nic.Speed; }
        catch (NetworkInformationException) { return 0; }
    }

    private static async Task<AdapterWatchResult> RunAsync(
        string jobId,
        string nameOrId,
        AdapterWatchOptions options,
        CancellationToken token,
        IProgress<NetworkProgress>? progress)
    {
        var nic = CounterSampleEngine.Find(nameOrId) ?? throw new InvalidOperationException("Adapter disappeared.");
        NetworkLog.Pending(HelperLog.Subcategories.Adapter, $"watchAdapter job={jobId} adapter={nic.Name} durationMs={(int)options.Duration.TotalMilliseconds}");
        var samples = new List<AdapterWatchSample> { Read(nic) };
        var first = samples[0].Status;
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

            nic = CounterSampleEngine.Find(nameOrId) ?? nic;
            var sample = Read(nic);
            samples.Add(sample);
            progress?.Report(new NetworkProgress
            {
                JobId = jobId,
                Phase = "Watch",
                Sent = samples.Count,
                LastStatus = sample.Status.ToString()
            });
            if (options.Interval is null)
                break;
        }

        var last = samples[^1].Status;
        var result = new AdapterWatchResult(jobId, nic.Name, nic.Id, clock.Elapsed, first, last, samples);
        NetworkLog.Success(HelperLog.Subcategories.Adapter, $"watchAdapter job={jobId} adapter={nic.Name} first={first} last={last} samples={samples.Count}");
        return result;
    }
}
