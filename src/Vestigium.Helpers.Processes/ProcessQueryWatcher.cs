using Vestigium.Helpers;
using Vestigium.Helpers.Kql;
using Vestigium.Logging;

namespace Vestigium.Helpers.Processes;

public sealed class ProcessQuerySample
{
    public required DateTimeOffset Timestamp { get; init; }
    public required TimeSpan Interval { get; init; }
    public required IReadOnlyList<ProcessInfo> Matches { get; init; }
}

public interface IProcessQueryWatcher : IDisposable
{
    string Query { get; }
    TimeSpan Interval { get; }
    event EventHandler<ProcessQuerySample>? Sampled;
}

internal sealed class ProcessQueryWatcher : IProcessQueryWatcher
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;
    private readonly KqlBoundQuery _query;
    private readonly KqlSession _session;
    private int _busy;
    private int _disposed;

    internal ProcessQueryWatcher(string query, TimeSpan interval, ProcessWatchFields fields)
    {
        Query = query;
        Interval = interval;
        Fields = fields == 0 ? ProcessWatchFields.All : fields;
        _session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile(query, _session);
        if (!compiled.Ok)
            throw new ArgumentException(compiled.Error?.Message ?? "query compile failed", nameof(query));
        _query = compiled.Query!;
        HelperLog.Information(
            HelperLog.AppIds.Processes,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Watch,
            $"Watch query start intervalMs={interval.TotalMilliseconds:0}");
        _loop = Task.Run(() => RunAsync(_cts.Token));
    }

    public string Query { get; }
    public TimeSpan Interval { get; }
    public ProcessWatchFields Fields { get; }
    public event EventHandler<ProcessQuerySample>? Sampled;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        _cts.Cancel();
        try { _loop.Wait(TimeSpan.FromSeconds(2)); } catch { }
        _cts.Dispose();
        _session.Dispose();
    }

    private async Task RunAsync(CancellationToken token)
    {
        await Task.Yield();
        Publish();
        using var timer = new PeriodicTimer(Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                if (Volatile.Read(ref _busy) != 0)
                    continue;
                Publish();
            }
        }
        catch (OperationCanceledException) { }
    }

    private void Publish()
    {
        if (Interlocked.Exchange(ref _busy, 1) != 0)
            return;
        try
        {
            if (Volatile.Read(ref _disposed) != 0)
                return;
            var hits = new List<ProcessInfo>();
            foreach (var row in ProcessSnapshotter.Capture(ProcessDetailLevel.Slim))
            {
                if (_query.Matches(new ProcessKqlRow(row)))
                    hits.Add(row);
            }

            Sampled?.Invoke(this, new ProcessQuerySample
            {
                Timestamp = DateTimeOffset.Now,
                Interval = Interval,
                Matches = hits
            });
        }
        catch { }
        finally { Volatile.Write(ref _busy, 0); }
    }
}
