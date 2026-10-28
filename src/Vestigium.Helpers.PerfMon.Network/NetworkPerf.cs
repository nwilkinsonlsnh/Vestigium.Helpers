namespace Vestigium.Helpers.PerfMon.Network;

/// <summary>Network façade. Shared SampleJob owns the clock and PDH reads.</summary>
public static class NetworkPerf
{
    public static Task<SampleJobResult> RunAsync(
        NetworkSampleOptions? options = null,
        CancellationToken cancellationToken = default)
    {
        options ??= new NetworkSampleOptions();
        NetworkPerfLog.Debug(NetworkPerfEvents.ProbeEnter, Vestigium.Logging.VestigiumStatus.Success, NetworkPerfCatalog.Subcategories.Probe, "enter probe");
        var paths = NetworkPaths.For(options);
        NetworkPerfLog.Debug(
            NetworkPerfEvents.PathsBuilt,
            Vestigium.Logging.VestigiumStatus.Success,
            NetworkPerfCatalog.Subcategories.Paths,
            "paths built",
            NetworkPerfLog.Props(("count", paths.Count.ToString())));
        NetworkPerfLog.Information(NetworkPerfEvents.ProbeStarted, Vestigium.Logging.VestigiumStatus.Success, NetworkPerfCatalog.Subcategories.Probe, "probe started");
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
                NetworkPerfLog.Information(NetworkPerfEvents.ProbeCancelled, Vestigium.Logging.VestigiumStatus.Success, NetworkPerfCatalog.Subcategories.Probe, "probe cancelled");
            else
                NetworkPerfLog.Information(NetworkPerfEvents.ProbeComplete, Vestigium.Logging.VestigiumStatus.Success, NetworkPerfCatalog.Subcategories.Probe, "probe complete");
            return result;
        }
        catch (ArgumentException)
        {
            NetworkPerfLog.Error(NetworkPerfEvents.ProbeRejected, Vestigium.Logging.VestigiumStatus.Failed, NetworkPerfCatalog.Subcategories.Probe, "probe rejected");
            throw;
        }
    }
}
