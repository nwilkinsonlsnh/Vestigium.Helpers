namespace Vestigium.Helpers.PerfMon.Cpu;

/// <summary>Cpu façade. Shared SampleJob owns the clock and PDH reads.</summary>
public static class CpuPerf
{
    public static Task<SampleJobResult> RunAsync(
        CpuSampleOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new CpuSampleOptions();
        CpuPerfLog.Debug(CpuPerfEvents.ProbeEnter, Vestigium.Logging.VestigiumStatus.Success, CpuPerfCatalog.Subcategories.Probe, "enter probe");
        var paths = CpuPaths.For(options);
        CpuPerfLog.Debug(
            CpuPerfEvents.PathsBuilt,
            Vestigium.Logging.VestigiumStatus.Success,
            CpuPerfCatalog.Subcategories.Paths,
            "paths built",
            CpuPerfLog.Props(("count", paths.Count.ToString())));
        CpuPerfLog.Information(CpuPerfEvents.ProbeStarted, Vestigium.Logging.VestigiumStatus.Success, CpuPerfCatalog.Subcategories.Probe, "probe started");
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
                CpuPerfLog.Information(CpuPerfEvents.ProbeCancelled, Vestigium.Logging.VestigiumStatus.Success, CpuPerfCatalog.Subcategories.Probe, "probe cancelled");
            else
                CpuPerfLog.Information(CpuPerfEvents.ProbeComplete, Vestigium.Logging.VestigiumStatus.Success, CpuPerfCatalog.Subcategories.Probe, "probe complete");
            return result;
        }
        catch (ArgumentException)
        {
            CpuPerfLog.Error(CpuPerfEvents.ProbeRejected, Vestigium.Logging.VestigiumStatus.Failed, CpuPerfCatalog.Subcategories.Probe, "probe rejected");
            throw;
        }
    }
}

