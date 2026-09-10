namespace Vestigium.Helpers.Network;

public enum NetworkJobStatus
{
    Pending,
    Running,
    Success,
    Failed,
    Cancelled,
    TimedOut
}

public sealed class NetworkProgress
{
    public string JobId { get; init; } = "";
    public string Phase { get; set; } = "Pending";
    public int Sent { get; set; }
    public int Received { get; set; }
    public int Sequence { get; set; }
    public string? LastStatus { get; set; }
    public long? LastRoundtripMs { get; set; }
}

public sealed class NetworkJob<TResult>
{
    readonly Func<CancellationToken, IProgress<NetworkProgress>?, Task<TResult>> _run;
    readonly CancellationTokenSource _cts = new();
    int _started;

    internal NetworkJob(
        string jobId,
        string kind,
        Func<CancellationToken, IProgress<NetworkProgress>?, Task<TResult>> run)
    {
        JobId = jobId;
        Kind = kind;
        Progress = new NetworkProgress { JobId = jobId };
        _run = run;
    }

    public string JobId { get; }
    public string Kind { get; }
    public NetworkProgress Progress { get; }
    public event EventHandler<NetworkProgress>? ProgressChanged;

    public void Cancel()
    {
        try
        {
            _cts.Cancel();
        }
        catch (ObjectDisposedException)
        {
        }
    }

    public async Task<TResult> RunAsync(CancellationToken cancellation = default)
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
            throw new InvalidOperationException("This NetworkJob has already been started.");

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _cts.Token);
        var progress = new Progress<NetworkProgress>(p =>
        {
            Progress.Phase = p.Phase;
            Progress.Sent = p.Sent;
            Progress.Received = p.Received;
            Progress.Sequence = p.Sequence;
            Progress.LastStatus = p.LastStatus;
            Progress.LastRoundtripMs = p.LastRoundtripMs;
            ProgressChanged?.Invoke(this, Progress);
        });
        return await _run(linked.Token, progress).ConfigureAwait(false);
    }
}
