namespace Vestigium.Helpers.PerfMon.Gpu;

/// <summary>Gpu façade. Shared SampleJob owns the clock and PDH reads.</summary>
public static class GpuPerf
{
    public static Task<SampleJobResult> RunAsync(
        GpuSampleOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new GpuSampleOptions();
        GpuPerfLog.Debug(GpuPerfEvents.ProbeEnter, Vestigium.Logging.VestigiumStatus.Success, GpuPerfCatalog.Subcategories.Probe, "enter probe");
        var paths = GpuPaths.For(options);
        if (paths.Count == 0)
            GpuPerfLog.Information(GpuPerfEvents.Headless, Vestigium.Logging.VestigiumStatus.Success, GpuPerfCatalog.Subcategories.Paths, "both short-job objects absent");
        GpuPerfLog.Debug(
            GpuPerfEvents.PathsBuilt,
            Vestigium.Logging.VestigiumStatus.Success,
            GpuPerfCatalog.Subcategories.Paths,
            "paths built",
            GpuPerfLog.Props(("count", paths.Count.ToString())));
        GpuPerfLog.Information(GpuPerfEvents.ProbeStarted, Vestigium.Logging.VestigiumStatus.Success, GpuPerfCatalog.Subcategories.Probe, "probe started");
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
        return Run(job, cancellationToken);
    }

    private static async Task<SampleJobResult> Run(SampleJob job, CancellationToken cancellationToken)
    {
        try
        {
            var result = await job.RunAsync(cancellationToken).ConfigureAwait(false);
            if (result.Status == SampleStatus.Cancelled)
                GpuPerfLog.Information(GpuPerfEvents.ProbeCancelled, Vestigium.Logging.VestigiumStatus.Success, GpuPerfCatalog.Subcategories.Probe, "probe cancelled");
            else
                GpuPerfLog.Information(GpuPerfEvents.ProbeComplete, Vestigium.Logging.VestigiumStatus.Success, GpuPerfCatalog.Subcategories.Probe, "probe complete");
            return result;
        }
        catch (ArgumentException)
        {
            GpuPerfLog.Error(GpuPerfEvents.ProbeRejected, Vestigium.Logging.VestigiumStatus.Failed, GpuPerfCatalog.Subcategories.Probe, "probe rejected");
            throw;
        }
    }
}
