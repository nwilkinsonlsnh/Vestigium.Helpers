using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Processes;

internal sealed class ProcessWatcher : IProcessWatcher
{
    private readonly CancellationTokenSource _cts = new();
    private readonly Task _loop;
    private int _busy;
    private int _disposed;
    private ProcessInfo? _previous;

    internal ProcessWatcher(int pid, TimeSpan interval, ProcessWatchFields fields)
    {
        Pid = pid;
        Interval = interval;
        Fields = fields == 0 ? ProcessWatchFields.All : fields;
        HelperLog.Information(
            HelperLog.AppIds.Processes,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Inventory,
            $"Watch start pid={pid} intervalMs={interval.TotalMilliseconds:0}");
        _loop = Task.Run(() => RunAsync(_cts.Token));
    }

    public int Pid { get; }
    public TimeSpan Interval { get; }
    public ProcessWatchFields Fields { get; }
    public event EventHandler<ProcessSample>? Sampled;
    public event EventHandler<EventArgs>? Exited;

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
            $"Watch stop pid={Pid}");
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

            var row = ProcessSnapshotter.CapturePid(Pid, ProcessDetailLevel.Slim);
            if (row is null)
            {
                Exited?.Invoke(this, EventArgs.Empty);
                _cts.Cancel();
                return;
            }

            TimeSpan? cpuDelta = null;
            double? cpuPercent = null;
            long? privateDelta = null, workingDelta = null;
            long? ioReadsDelta = null, ioReadBytesDelta = null, ioWritesDelta = null, ioWriteBytesDelta = null;

            if (_previous is { } prior)
            {
                if (Fields.HasFlag(ProcessWatchFields.Cpu) && row.CpuTime is { } nowCpu && prior.CpuTime is { } thenCpu)
                {
                    cpuDelta = nowCpu - thenCpu;
                    var seconds = Interval.TotalSeconds * Math.Max(1, Environment.ProcessorCount);
                    if (seconds > 0)
                        cpuPercent = 100.0 * Math.Max(0, cpuDelta.Value.TotalSeconds) / seconds;
                }
                if (Fields.HasFlag(ProcessWatchFields.PrivateBytes))
                    privateDelta = Delta(row.PrivateBytes, prior.PrivateBytes);
                if (Fields.HasFlag(ProcessWatchFields.WorkingSet))
                    workingDelta = Delta(row.WorkingSet, prior.WorkingSet);
                if (Fields.HasFlag(ProcessWatchFields.IoReads))
                    ioReadsDelta = Delta(row.IoReads, prior.IoReads);
                if (Fields.HasFlag(ProcessWatchFields.IoReadBytes))
                    ioReadBytesDelta = Delta(row.IoReadBytes, prior.IoReadBytes);
                if (Fields.HasFlag(ProcessWatchFields.IoWrites))
                    ioWritesDelta = Delta(row.IoWrites, prior.IoWrites);
                if (Fields.HasFlag(ProcessWatchFields.IoWriteBytes))
                    ioWriteBytesDelta = Delta(row.IoWriteBytes, prior.IoWriteBytes);
            }

            _previous = row;
            Sampled?.Invoke(this, new ProcessSample
            {
                Process = row,
                Interval = Interval,
                Timestamp = DateTimeOffset.Now,
                CpuPercent = cpuPercent,
                CpuTimeDelta = cpuDelta,
                PrivateBytesDelta = privateDelta,
                WorkingSetDelta = workingDelta,
                IoReadsDelta = ioReadsDelta,
                IoReadBytesDelta = ioReadBytesDelta,
                IoWritesDelta = ioWritesDelta,
                IoWriteBytesDelta = ioWriteBytesDelta
            });
        }
        catch { }
        finally { Volatile.Write(ref _busy, 0); }
    }

    private static long? Delta(long? now, long? then)
        => now is { } a && then is { } b ? a - b : null;
}
