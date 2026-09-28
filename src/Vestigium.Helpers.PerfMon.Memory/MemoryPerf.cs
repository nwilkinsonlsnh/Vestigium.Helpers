namespace Vestigium.Helpers.PerfMon.Memory;

/// <summary>Memory façade. Shared SampleJob owns the clock and PDH reads.</summary>
public static class MemoryPerf
{
    public static Task<SampleJobResult> RunAsync(
        MemorySampleOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new MemorySampleOptions();
        var paths = MemoryPaths.For(options);
        var job = new SampleJob(paths, new SampleJobOptions
        {
            Interval = options.Interval,
            Duration = options.Duration,
            Count = options.Count,
            AllowBurst = options.AllowBurst,
            Clock = options.Clock,
            Source = options.Source
        });
        return job.RunAsync(cancellationToken);
    }
}
