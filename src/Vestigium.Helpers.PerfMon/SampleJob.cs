namespace Vestigium.Helpers.PerfMon;

/// <summary>Bounded sample clock. Default source is <see cref="CachedPdhSource"/>.</summary>
public sealed class SampleJob
{
    public SampleJob(IReadOnlyList<CounterPath> paths, SampleJobOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if (paths.Count == 0)
        {
            PerfMonLog.Error(PerfMonEvents.JobRejected, Vestigium.Logging.VestigiumStatus.Failed, PerfMonCatalog.Subcategories.Job, "empty path list");
            throw new ArgumentException("Path list cannot be empty.", nameof(paths));
        }

        Paths = paths.ToArray();
        Options = SampleJobOptions.Normalize(options);
    }

    public IReadOnlyList<CounterPath> Paths { get; }
    public SampleJobOptions Options { get; }

    public async Task<SampleJobResult> RunAsync(CancellationToken cancellationToken = default)
    {
        PerfMonLog.Debug(PerfMonEvents.JobEnter, Vestigium.Logging.VestigiumStatus.Success, PerfMonCatalog.Subcategories.Job, "enter job");
        Options.RejectIfUnbounded(cancellationToken);
        var owned = Options.Source is null;
        var source = Options.Source ?? new CachedPdhSource();

        var samples = new List<SampleRecord>();
        var terminal = SampleStatus.Ok;
        var started = Options.Clock.GetUtcNow();
        var ticks = 0;

        try
        {
            try
            {
                Prime(source, cancellationToken);
                PerfMonLog.Information(PerfMonEvents.JobStarted, Vestigium.Logging.VestigiumStatus.Success, PerfMonCatalog.Subcategories.Job, "job started");
            }
            catch (OperationCanceledException)
            {
                return Result(SampleStatus.Cancelled, samples);
            }

            while (true)
            {
                if (cancellationToken.IsCancellationRequested)
                    return Result(SampleStatus.Cancelled, samples);

                if (CountReached(ticks) || DurationReached(started))
                    break;

                var tickOk = false;
                var tickMiss = false;
                foreach (var path in Paths)
                {
                    if (cancellationToken.IsCancellationRequested)
                        return Result(SampleStatus.Cancelled, samples);

                    var row = source.Read(path);
                    samples.Add(row);
                    if (row.Status == SampleStatus.Unavailable)
                        tickMiss = true;
                    else
                        tickOk = true;
                }

                ticks++;
                PerfMonLog.Debug(
                    PerfMonEvents.JobTick,
                    Vestigium.Logging.VestigiumStatus.Success,
                    PerfMonCatalog.Subcategories.Job,
                    "job tick",
                    properties: PerfMonLog.Props(("ticks", ticks.ToString()), ("status", terminal.ToString())));
                if (tickMiss && tickOk)
                    terminal = SampleStatus.Partial;

                if (CountReached(ticks) || DurationReached(started))
                    break;

                if (cancellationToken.IsCancellationRequested)
                    return Result(SampleStatus.Cancelled, samples);

                try
                {
                    await Task.Delay(Options.Interval, Options.Clock, cancellationToken).ConfigureAwait(false);
                }
                catch (OperationCanceledException)
                {
                    return Result(SampleStatus.Cancelled, samples);
                }
            }

            return Result(terminal, samples);
        }
        finally
        {
            if (owned && source is IDisposable disposable)
                disposable.Dispose();
        }
    }

    private void Prime(ICounterSource source, CancellationToken cancellationToken)
    {
        foreach (var path in Paths)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!source.NeedsPrime(path))
                continue;
            _ = source.Read(path);
        }
    }

    private bool CountReached(int ticks)
        => Options.HasCountLimit && ticks >= Options.Count!.Value;

    private bool DurationReached(DateTimeOffset started)
        => Options.HasDurationLimit && Options.Clock.GetUtcNow() - started >= Options.Duration;

    private static SampleJobResult Result(SampleStatus status, List<SampleRecord> samples)
    {
        if (status == SampleStatus.Cancelled)
            PerfMonLog.Information(PerfMonEvents.JobCancelled, Vestigium.Logging.VestigiumStatus.Success, PerfMonCatalog.Subcategories.Job, "job cancelled");
        else if (status == SampleStatus.Rejected)
            PerfMonLog.Error(PerfMonEvents.JobRejected, Vestigium.Logging.VestigiumStatus.Failed, PerfMonCatalog.Subcategories.Job, "job rejected");
        else
            PerfMonLog.Information(PerfMonEvents.JobComplete, Vestigium.Logging.VestigiumStatus.Success, PerfMonCatalog.Subcategories.Job, "job complete");
        return new SampleJobResult(status, samples.ToArray());
    }
}
