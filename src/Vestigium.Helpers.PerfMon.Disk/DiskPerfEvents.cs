namespace Vestigium.Helpers.PerfMon.Disk;

/// <summary>Custom catalog block 19500–19999 (count by 5). Used IDs this plan: 19500–19545.</summary>
public static class DiskPerfEvents
{
    public const int BlockStart = 19500;
    public const int BlockEnd = 19999;

    public const int ProbeEnter = 19500;
    public const int ProbeStarted = 19505;
    public const int PathsBuilt = 19510;
    public const int ProbeComplete = 19515;
    public const int ProbeCancelled = 19520;
    public const int ProbeRejected = 19525;
    public const int ObjectMissing = 19530;
    public const int DisksCapped = 19535;
    public const int CounterOmitted = 19540;
    public const int ProbeFailed = 19545;
}
