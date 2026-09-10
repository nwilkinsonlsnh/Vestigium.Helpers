namespace Vestigium.Helpers.Processes;

public enum ThreadState
{
    Unknown = 0,
    Initialized = 1,
    Ready = 2,
    Running = 3,
    Standby = 4,
    Terminated = 5,
    Waiting = 6,
    Transition = 7
}

/// <summary>One thread snapshot. Stack is omitted unless includeStack is set.</summary>
public sealed class ThreadInfo
{
    public int ThreadId { get; init; }
    public int ProcessId { get; init; }
    public DateTimeOffset? StartTime { get; init; }
    public ThreadState State { get; init; }
    public string? WaitReason { get; init; }
    public string? StartAddress { get; init; }
    public string? StartModule { get; init; }
    public string? Stack { get; init; }
    public double? CpuPercent { get; init; }
    public long? ContextSwitches { get; init; }
    public long? ContextSwitchDelta { get; init; }
    public int? SuspendCount { get; init; }
    public TimeSpan? KernelTime { get; init; }
    public TimeSpan? UserTime { get; init; }
    public long? Cycles { get; init; }
    public int? BasePriority { get; init; }
    public int? DynamicPriority { get; init; }
    public int? IoPriority { get; init; }
    public int? MemoryPriority { get; init; }
    public int? IdealProcessor { get; init; }
}
