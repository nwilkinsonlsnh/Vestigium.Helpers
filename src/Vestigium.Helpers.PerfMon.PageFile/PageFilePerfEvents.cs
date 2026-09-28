namespace Vestigium.Helpers.PerfMon.PageFile;

/// <summary>Custom catalog block 20000–20499 (count by 5). Used IDs this plan: 20000–20045.</summary>
public static class PageFilePerfEvents
{
    public const int BlockStart = 20000;
    public const int BlockEnd = 20499;

    public const int ProbeEnter = 20000;
    public const int ProbeStarted = 20005;
    public const int PathsBuilt = 20010;
    public const int ProbeComplete = 20015;
    public const int ProbeCancelled = 20020;
    public const int ProbeRejected = 20025;
    public const int ObjectMissing = 20030;
    public const int FilesCapped = 20035;
    public const int SiblingSkipped = 20040;
    public const int ProbeFailed = 20045;
}
