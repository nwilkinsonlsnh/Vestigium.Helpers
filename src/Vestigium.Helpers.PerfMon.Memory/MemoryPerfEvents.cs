namespace Vestigium.Helpers.PerfMon.Memory;

/// <summary>Custom catalog block 19000–19499 (count by 5). Used IDs this plan: 19000–19045.</summary>
public static class MemoryPerfEvents
{
    public const int BlockStart = 19000;
    public const int BlockEnd = 19499;

    public const int ProbeEnter = 19000;
    public const int ProbeStarted = 19005;
    public const int PathsBuilt = 19010;
    public const int ProbeComplete = 19015;
    public const int ProbeCancelled = 19020;
    public const int ProbeRejected = 19025;
    public const int ObjectMissing = 19030;
    public const int SiblingSkipped = 19035;
    public const int OptionalAbsent = 19040;
    public const int ProbeFailed = 19045;
}
