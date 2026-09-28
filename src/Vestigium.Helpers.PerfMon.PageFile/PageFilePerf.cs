namespace Vestigium.Helpers.PerfMon.PageFile;

/// <summary>PageFile façade. Shared SampleJob owns the clock and PDH reads.</summary>
public static class PageFilePerf
{
    public static Task<SampleJobResult> RunAsync(
        PageFileSampleOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new PageFileSampleOptions();
        var paths = PageFilePaths.For(options);
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
