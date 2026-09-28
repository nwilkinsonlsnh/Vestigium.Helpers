namespace Vestigium.Helpers.PerfMon.Network;

/// <summary>Custom catalog block 17500–17999 (count by 5). Used IDs this plan: 17500–17545.</summary>
public static class NetworkPerfEvents
{
    public const int BlockStart = 17500;
    public const int BlockEnd = 17999;

    public const int ProbeEnter = 17500;
    public const int ProbeStarted = 17505;
    public const int PathsBuilt = 17510;
    public const int ProbeComplete = 17515;
    public const int ProbeCancelled = 17520;
    public const int ProbeRejected = 17525;
    public const int ObjectMissing = 17530;
    public const int AdaptersCapped = 17535;
    public const int OptionalAbsent = 17540;
    public const int ProbeFailed = 17545;
}
