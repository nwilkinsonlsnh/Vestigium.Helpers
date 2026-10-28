namespace Vestigium.Helpers.PerfMon.Cpu;

/// <summary>PDH category Processor. Generated from EventCatalog/pdh-categories.json.</summary>
public static class Processor
{
    public const string Category = "Processor";
    public const string PercentC1Time = "% C1 Time";
    public const string PercentC2Time = "% C2 Time";
    public const string PercentC3Time = "% C3 Time";
    public const string PercentDPCTime = "% DPC Time";
    public const string PercentIdleTime = "% Idle Time";
    public const string PercentInterruptTime = "% Interrupt Time";
    public const string PercentPrivilegedTime = "% Privileged Time";
    public const string PercentProcessorTime = "% Processor Time";
    public const string PercentUserTime = "% User Time";
    public const string C1TransitionsPerSec = "C1 Transitions/sec";
    public const string C2TransitionsPerSec = "C2 Transitions/sec";
    public const string C3TransitionsPerSec = "C3 Transitions/sec";
    public const string DPCRate = "DPC Rate";
    public const string DPCsQueuedPerSec = "DPCs Queued/sec";
    public const string InterruptsPerSec = "Interrupts/sec";

    public static IReadOnlyList<string> Counters { get; } =
    [
        PercentC1Time,
        PercentC2Time,
        PercentC3Time,
        PercentDPCTime,
        PercentIdleTime,
        PercentInterruptTime,
        PercentPrivilegedTime,
        PercentProcessorTime,
        PercentUserTime,
        C1TransitionsPerSec,
        C2TransitionsPerSec,
        C3TransitionsPerSec,
        DPCRate,
        DPCsQueuedPerSec,
        InterruptsPerSec,
    ];
}
