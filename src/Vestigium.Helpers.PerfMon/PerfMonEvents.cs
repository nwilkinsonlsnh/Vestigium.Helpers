namespace Vestigium.Helpers.PerfMon;

/// <summary>Custom catalog block 17000–17499 (count by 5). Used IDs this plan: 17000–17045.</summary>
public static class PerfMonEvents
{
    public const int BlockStart = 17000;
    public const int BlockEnd = 17499;

    public const int JobEnter = 17000;
    public const int JobStarted = 17005;
    public const int JobTick = 17010;
    public const int JobComplete = 17015;
    public const int JobCancelled = 17020;
    public const int JobRejected = 17025;
    public const int SourceUnavailable = 17030;
    public const int SourceThrown = 17035;
    public const int PathRejected = 17040;
    public const int JobFailed = 17045;
}
