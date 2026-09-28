namespace Vestigium.Helpers.PerfMon.Disk;

/// <summary>Disk façade. Shared SampleJob owns the clock and PDH reads.</summary>
public static class DiskPerf
{
    public static Task<SampleJobResult> RunAsync(
        DiskSampleOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new DiskSampleOptions();
        var paths = DiskPaths.For(options);
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
