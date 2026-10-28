namespace Vestigium.Helpers.Processes;

[Flags]
public enum ProcessWatchFields
{
    Cpu = 1,
    PrivateBytes = 2,
    WorkingSet = 4,
    IoReads = 8,
    IoReadBytes = 16,
    IoWrites = 32,
    IoWriteBytes = 64,
    Gpu = 128,
    All = Cpu | PrivateBytes | WorkingSet | IoReads | IoReadBytes | IoWrites | IoWriteBytes | Gpu
}

public sealed class ProcessSample
{
    public required ProcessInfo Process { get; init; }
    public required TimeSpan Interval { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
    public double? CpuPercent { get; init; }
    public TimeSpan? CpuTimeDelta { get; init; }
    public long? PrivateBytesDelta { get; init; }
    public long? WorkingSetDelta { get; init; }
    public long? IoReadsDelta { get; init; }
    public long? IoReadBytesDelta { get; init; }
    public long? IoWritesDelta { get; init; }
    public long? IoWriteBytesDelta { get; init; }
}

public interface IProcessWatcher : IDisposable
{
    int Pid { get; }
    TimeSpan Interval { get; }
    ProcessWatchFields Fields { get; }
    event EventHandler<ProcessSample>? Sampled;
    event EventHandler<EventArgs>? Exited;
}

public interface ISystemWatcher : IDisposable
{
    TimeSpan Interval { get; }
    event EventHandler<SystemCounters>? Sampled;
}
