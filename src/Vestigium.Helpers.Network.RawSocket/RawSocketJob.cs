using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Network.RawSocket;

public sealed class RawSocketJob<TResult>
{
    private readonly Func<CancellationToken, IProgress<NetworkProgress>?, Task<TResult>> _run;
    private readonly CancellationTokenSource _cts = new();
    private int _started;

    internal RawSocketJob(string jobId, string kind, Func<CancellationToken, IProgress<NetworkProgress>?, Task<TResult>> run)
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
        try { _cts.Cancel(); }
        catch (ObjectDisposedException) { }
    }

    public async Task<TResult> RunAsync(CancellationToken cancellation = default)
    {
        if (Interlocked.Exchange(ref _started, 1) == 1)
            throw new InvalidOperationException("This RawSocketJob has already been started.");

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
