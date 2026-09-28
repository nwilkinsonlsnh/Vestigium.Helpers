namespace Vestigium.Helpers.PerfMon.Gpu;

/// <summary>Custom catalog block 18500–18999 (count by 5). Used IDs this plan: 18500–18545.</summary>
public static class GpuPerfEvents
{
    public const int BlockStart = 18500;
    public const int BlockEnd = 18999;

    public const int ProbeEnter = 18500;
    public const int ProbeStarted = 18505;
    public const int PathsBuilt = 18510;
    public const int ProbeComplete = 18515;
    public const int ProbeCancelled = 18520;
    public const int ProbeRejected = 18525;
    public const int ObjectMissing = 18530;
    public const int InstancesCapped = 18535;
    public const int Headless = 18540;
    public const int ProbeFailed = 18545;
}
