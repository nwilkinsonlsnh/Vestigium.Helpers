namespace Vestigium.Helpers.PerfMon.Memory;

/// <summary>Memory façade. Shared SampleJob owns the clock and PDH reads.</summary>
public static class MemoryPerf
{
    /// <summary>
    /// The short Memory object list a host chart needs. Names come from the catalog, not the host.
    /// </summary>
    public static IReadOnlyList<CounterPath> HostPaths()
        =>
        [
            new CounterPath(Memory.Category, Memory.AvailableMBytes, string.Empty, "MB"),
            new CounterPath(Memory.Category, Memory.CommittedBytes, string.Empty, "bytes"),
            new CounterPath(Memory.Category, Memory.PercentCommittedBytesInUse, string.Empty, "%"),
            new CounterPath(Memory.Category, Memory.CommitLimit, string.Empty, "bytes"),
            new CounterPath(Memory.Category, Memory.CacheBytes, string.Empty, "bytes")
        ];

    public static Task<SampleJobResult> RunAsync(
        MemorySampleOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new MemorySampleOptions();
        MemoryPerfLog.Debug(MemoryPerfEvents.ProbeEnter, Vestigium.Logging.VestigiumStatus.Success, MemoryPerfCatalog.Subcategories.Probe, "enter probe");
        var paths = MemoryPaths.For(options);
        MemoryPerfLog.Debug(
            MemoryPerfEvents.PathsBuilt,
            Vestigium.Logging.VestigiumStatus.Success,
            MemoryPerfCatalog.Subcategories.Paths,
            "paths built",
            MemoryPerfLog.Props(("count", paths.Count.ToString())));
        MemoryPerfLog.Information(MemoryPerfEvents.ProbeStarted, Vestigium.Logging.VestigiumStatus.Success, MemoryPerfCatalog.Subcategories.Probe, "probe started");
        var job = new SampleJob(paths, new SampleJobOptions
        {
            Interval = options.Interval,
            Duration = options.Duration,
            Count = options.Count,
            AllowBurst = options.AllowBurst,
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
                MemoryPerfLog.Information(MemoryPerfEvents.ProbeCancelled, Vestigium.Logging.VestigiumStatus.Success, MemoryPerfCatalog.Subcategories.Probe, "probe cancelled");
            else
                MemoryPerfLog.Information(MemoryPerfEvents.ProbeComplete, Vestigium.Logging.VestigiumStatus.Success, MemoryPerfCatalog.Subcategories.Probe, "probe complete");
            return result;
        }
        catch (ArgumentException)
        {
            MemoryPerfLog.Error(MemoryPerfEvents.ProbeRejected, Vestigium.Logging.VestigiumStatus.Failed, MemoryPerfCatalog.Subcategories.Probe, "probe rejected");
            throw;
        }
    }
}
