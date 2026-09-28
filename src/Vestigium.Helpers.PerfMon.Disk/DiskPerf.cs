namespace Vestigium.Helpers.PerfMon.Disk;

/// <summary>Disk façade. Shared SampleJob owns the clock and PDH reads.</summary>
public static class DiskPerf
{
    public static Task<SampleJobResult> RunAsync(
        DiskSampleOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new DiskSampleOptions();
        DiskPerfLog.Debug(DiskPerfEvents.ProbeEnter, Vestigium.Logging.VestigiumStatus.Success, DiskPerfCatalog.Subcategories.Probe, "enter probe");
        var paths = DiskPaths.For(options);
        DiskPerfLog.Debug(
            DiskPerfEvents.PathsBuilt,
            Vestigium.Logging.VestigiumStatus.Success,
            DiskPerfCatalog.Subcategories.Paths,
            "paths built",
            DiskPerfLog.Props(("count", paths.Count.ToString())));
        DiskPerfLog.Information(DiskPerfEvents.ProbeStarted, Vestigium.Logging.VestigiumStatus.Success, DiskPerfCatalog.Subcategories.Probe, "probe started");
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
                DiskPerfLog.Information(DiskPerfEvents.ProbeCancelled, Vestigium.Logging.VestigiumStatus.Success, DiskPerfCatalog.Subcategories.Probe, "probe cancelled");
            else
                DiskPerfLog.Information(DiskPerfEvents.ProbeComplete, Vestigium.Logging.VestigiumStatus.Success, DiskPerfCatalog.Subcategories.Probe, "probe complete");
            return result;
        }
        catch (ArgumentException)
        {
            DiskPerfLog.Error(DiskPerfEvents.ProbeRejected, Vestigium.Logging.VestigiumStatus.Failed, DiskPerfCatalog.Subcategories.Probe, "probe rejected");
            throw;
        }
    }
}
