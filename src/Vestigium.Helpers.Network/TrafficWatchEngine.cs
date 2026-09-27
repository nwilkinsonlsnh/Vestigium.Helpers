using System.Diagnostics;
using System.Net.NetworkInformation;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class TrafficWatchEngine
{
    public static NetworkJob<TrafficWatchResult> Create(string nameOrId, TrafficWatchOptions? options)
    {
        var key = HelperGuard.NotBlank(nameOrId, nameof(nameOrId)).Trim();
        var o = options ?? new TrafficWatchOptions();
        if (o.Duration < TrafficWatchOptions.MinDuration || o.Duration > TrafficWatchOptions.MaxDuration)
            throw new ArgumentOutOfRangeException(nameof(o.Duration), "Duration must be between 10 ms and 1 hour.");
        if (o.Interval <= TimeSpan.Zero || o.Interval > o.Duration)
            throw new ArgumentOutOfRangeException(nameof(o.Interval), "Interval must be greater than 0 and at most Duration.");
        if (CounterSampleEngine.Find(key) is null)
            throw new ArgumentException("Adapter was not found.", nameof(nameOrId));

        var jobId = "traffic-" + HelperLog.NewId();
        return new NetworkJob<TrafficWatchResult>(jobId, "trafficWatch", (token, progress) => RunAsync(jobId, key, o, token, progress));
    }

    public static CounterReading Read(string nameOrId)
    {
        var nic = CounterSampleEngine.Find(nameOrId) ?? throw new ArgumentException("Adapter was not found.", nameof(nameOrId));
        return CounterSampleEngine.Read(nic);
    }

    private static async Task<TrafficWatchResult> RunAsync(
        string jobId,
        string key,
        TrafficWatchOptions options,
        CancellationToken token,
        IProgress<NetworkProgress>? progress)
    {
        var nic = CounterSampleEngine.Find(key) ?? throw new InvalidOperationException("Adapter disappeared.");
        var previous = CounterSampleEngine.Read(nic);
        var firstStatus = nic.OperationalStatus;
        var lastStatus = firstStatus;
        var samples = new List<TrafficSample>();
        var clock = Stopwatch.StartNew();

        while (clock.Elapsed < options.Duration && !token.IsCancellationRequested)
        {
            var remain = options.Duration - clock.Elapsed;
            var wait = remain < options.Interval ? remain : options.Interval;
            if (wait > TimeSpan.Zero)
            {
                try { await Task.Delay(wait, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }

            nic = CounterSampleEngine.Find(key) ?? nic;
            var now = CounterSampleEngine.Read(nic);
            var delta = CounterSampleEngine.Delta(previous, now);
            var seconds = wait.TotalSeconds <= 0 ? options.Interval.TotalSeconds : wait.TotalSeconds;
            lastStatus = nic.OperationalStatus;
            var sample = new TrafficSample(
                DateTimeOffset.UtcNow,
                lastStatus,
                BitsPerSecond(delta.BytesIn, seconds),
                BitsPerSecond(delta.BytesOut, seconds),
                delta.ErrorsIn,
                delta.ErrorsOut,
                delta.DiscardsIn,
                delta.DiscardsOut);
            samples.Add(sample);
            previous = now;
            progress?.Report(new NetworkProgress
            {
                JobId = jobId,
                Phase = "Traffic",
                Sent = samples.Count,
                LastStatus = lastStatus.ToString()
            });
        }

        return new TrafficWatchResult(jobId, nic.Name, nic.Id, clock.Elapsed, firstStatus, lastStatus, samples);
    }

    private static long BitsPerSecond(long bytes, double seconds)
    {
        if (seconds <= 0)
            return 0;
        return (long)(bytes * 8d / seconds);
    }
}
