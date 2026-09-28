namespace Vestigium.Helpers.PerfMon.PageFile;

/// <summary>PageFile façade. Shared SampleJob owns the clock and PDH reads.</summary>
public static class PageFilePerf
{
    public static Task<SampleJobResult> RunAsync(
        PageFileSampleOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new PageFileSampleOptions();
        PageFilePerfLog.Debug(PageFilePerfEvents.ProbeEnter, Vestigium.Logging.VestigiumStatus.Success, PageFilePerfCatalog.Subcategories.Probe, "enter probe");
        var paths = PageFilePaths.For(options);
        PageFilePerfLog.Debug(
            PageFilePerfEvents.PathsBuilt,
            Vestigium.Logging.VestigiumStatus.Success,
            PageFilePerfCatalog.Subcategories.Paths,
            "paths built",
            PageFilePerfLog.Props(("count", paths.Count.ToString())));
        PageFilePerfLog.Information(PageFilePerfEvents.ProbeStarted, Vestigium.Logging.VestigiumStatus.Success, PageFilePerfCatalog.Subcategories.Probe, "probe started");
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
                PageFilePerfLog.Information(PageFilePerfEvents.ProbeCancelled, Vestigium.Logging.VestigiumStatus.Success, PageFilePerfCatalog.Subcategories.Probe, "probe cancelled");
            else
                PageFilePerfLog.Information(PageFilePerfEvents.ProbeComplete, Vestigium.Logging.VestigiumStatus.Success, PageFilePerfCatalog.Subcategories.Probe, "probe complete");
            return result;
        }
        catch (ArgumentException)
        {
            PageFilePerfLog.Error(PageFilePerfEvents.ProbeRejected, Vestigium.Logging.VestigiumStatus.Failed, PageFilePerfCatalog.Subcategories.Probe, "probe rejected");
            throw;
        }
    }
}
