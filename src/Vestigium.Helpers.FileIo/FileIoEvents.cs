namespace Vestigium.Helpers.FileIo;

/// <summary>Custom catalog block 12500–12999 (count by 5).</summary>
public static class FileIoEvents
{
    public const int BlockStart = 12500;
    public const int BlockEnd = 12999;

    public const int ProbeEnter = 12500;
    public const int ProbeComplete = 12505;
    public const int OperationEnter = 12510;
    public const int OperationComplete = 12515;
    public const int OperationFailed = 12520;
    public const int OperationWarning = 12525;
    public const int PathRejected = 12530;
    public const int JobStart = 12535;
    public const int JobComplete = 12540;
    public const int JobCancelled = 12545;
    public const int ReconStart = 12550;
    public const int ReconComplete = 12555;
    public const int ConsumersReleased = 12560;
    public const int Decision = 12565;
    public const int NameCap = 12570;
    public const int ItemInUse = 12575;
    public const int ItemUnauthorized = 12580;
    public const int IndexBuilt = 12585;
    public const int IndexHit = 12590;
    public const int ProgressSnapshot = 12595;
    public const int StatsFinalize = 12600;
    public const int JobPaused = 12605;
    public const int JobResumed = 12610;
}
