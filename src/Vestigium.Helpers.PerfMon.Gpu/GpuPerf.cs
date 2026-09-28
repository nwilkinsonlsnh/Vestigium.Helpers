namespace Vestigium.Helpers.PerfMon.Gpu;

/// <summary>Gpu façade. Shared SampleJob owns the clock and PDH reads.</summary>
public static class GpuPerf
{
    public static Task<SampleJobResult> RunAsync(
        GpuSampleOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new GpuSampleOptions();
        var paths = GpuPaths.For(options);
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
