namespace Vestigium.Helpers.PerfMon;

/// <summary>Bounded sample clock. Default source is local PDH.</summary>
public sealed class SampleJob
{
    public SampleJob(IReadOnlyList<CounterPath> paths, SampleJobOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(paths);
        if (paths.Count == 0)
            throw new ArgumentException("Path list cannot be empty.", nameof(paths));

        Paths = paths.ToArray();
        Options = SampleJobOptions.Normalize(options);
    }

    public IReadOnlyList<CounterPath> Paths { get; }
    public SampleJobOptions Options { get; }

    public async Task<SampleJobResult> RunAsync(CancellationToken cancellationToken = default)
    {
        Options.RejectIfUnbounded(cancellationToken);
        var source = Options.Source ?? new PerformanceCounterSource();

        var samples = new List<SampleRecord>();
        var terminal = SampleStatus.Ok;
        var started = Options.Clock.GetUtcNow();
        var ticks = 0;

        try
        {
            Prime(source, cancellationToken);
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
        => new(status, samples.ToArray());
}
