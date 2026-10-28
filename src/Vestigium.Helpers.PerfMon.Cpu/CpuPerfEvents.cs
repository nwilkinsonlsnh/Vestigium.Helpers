namespace Vestigium.Helpers.PerfMon.Cpu;

/// <summary>Custom catalog block 18000–18499 (count by 5). Used IDs this plan: 18000–18045.</summary>
public static class CpuPerfEvents
{
    public const int BlockStart = 18000;
    public const int BlockEnd = 18499;

    public const int ProbeEnter = 18000;
    public const int ProbeStarted = 18005;
    public const int PathsBuilt = 18010;
    public const int ProbeComplete = 18015;
    public const int ProbeCancelled = 18020;
    public const int ProbeRejected = 18025;
    public const int CategoryFallback = 18030;
    public const int ParkingSkipped = 18035;
    public const int CoresCapped = 18040;
    public const int ProbeFailed = 18045;
}
