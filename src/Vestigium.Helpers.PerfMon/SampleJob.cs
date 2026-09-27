namespace Vestigium.Helpers.PerfMon;

/// <summary>
/// Bounded sample clock. PM01.003 is the door. The loop lands in PM01.004.
/// </summary>
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

    public Task<SampleJobResult> RunAsync(CancellationToken cancellationToken = default)
    {
        Options.RejectIfUnbounded(cancellationToken);
        return Task.FromResult(new SampleJobResult(SampleStatus.Ok, Array.Empty<SampleRecord>()));
    }
}
