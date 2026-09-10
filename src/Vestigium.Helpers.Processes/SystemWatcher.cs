using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Processes;

internal sealed class SystemWatcher : ISystemWatcher
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;
    private int _busy;
    private int _disposed;
    private SystemTimes? _previous;

    internal SystemWatcher(TimeSpan interval)
    {
        Interval = interval;
        HelperLog.Information(
            HelperLog.AppIds.Processes,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Inventory,
            $"WatchSystem start intervalMs={interval.TotalMilliseconds:0}");
        _loop = Task.Run(() => RunAsync(_cts.Token));
    }

    public TimeSpan Interval { get; }
    public event EventHandler<SystemCounters>? Sampled;

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        _cts.Cancel();
        try { _loop.Wait(TimeSpan.FromSeconds(2)); } catch { }
        _cts.Dispose();
        HelperLog.Information(
            HelperLog.AppIds.Processes,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Inventory,
            "WatchSystem stop");
    }

    private async Task RunAsync(CancellationToken token)
    {
        await Task.Yield();
        Publish(first: true);
        using var timer = new PeriodicTimer(Interval);
        try
        {
            while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
            {
                if (Volatile.Read(ref _busy) != 0)
                    continue;
                Publish(first: false);
            }
        }
        catch (OperationCanceledException) { }
    }

    private void Publish(bool first)
    {
        if (Interlocked.Exchange(ref _busy, 1) != 0)
            return;
        try
        {
            if (Volatile.Read(ref _disposed) != 0)
                return;
            var prior = first ? null : _previous;
            var sample = SystemCounterReader.Capture(prior, first ? null : Interval);
            _previous = SystemCounterReader.ReadTimes();
            Sampled?.Invoke(this, sample);
        }
        catch { }
        finally { Volatile.Write(ref _busy, 0); }
    }
}
