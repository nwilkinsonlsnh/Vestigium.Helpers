namespace Vestigium.Helpers.PerfMon.Network;

/// <summary>Network façade. Shared SampleJob owns the clock and PDH reads.</summary>
public static class NetworkPerf
{
    public static Task<SampleJobResult> RunAsync(
        NetworkSampleOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new NetworkSampleOptions();
        var paths = NetworkPaths.For(options);
        var job = new SampleJob(paths, new SampleJobOptions
        {
            Interval = options.Interval,
            Duration = options.Duration,
            Count = options.Count,
            AllowBurst = options.AllowBurst,
            InstanceCap = options.InstanceCap,
            Clock = options.Clock,
            Source = options.Source
        });
        return job.RunAsync(cancellationToken);
    }
}
