namespace Vestigium.Helpers.PerfMon.Cpu;

/// <summary>Cpu façade. Shared SampleJob owns the clock and PDH reads.</summary>
public static class CpuPerf
{
    public static Task<SampleJobResult> RunAsync(
        CpuSampleOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new CpuSampleOptions();
        var paths = CpuPaths.For(options);
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
