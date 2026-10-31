namespace Vestigium.Helpers.Network.RawSocket;

public sealed class RawSocketJob<TResult>
{
    private readonly Func<CancellationToken, Task<TResult>> _run;
    private readonly CancellationTokenSource _cts = new();
    private int _started;

    internal RawSocketJob(string jobId, string kind, Func<CancellationToken, Task<TResult>> run)
    {
        JobId = jobId;
        Kind = kind;
        _run = run;
    }

    public string JobId { get; }
    public string Kind { get; }

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
        return await _run(linked.Token).ConfigureAwait(false);
    }
}
