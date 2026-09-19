namespace Vestigium.Helpers.Network;

/// <summary>Custom catalog block 14500–14999 (count by 5).</summary>
public static class NetworkEvents
{
    public const int BlockStart = 14500;
    public const int BlockEnd = 14999;

    public const int ProbeEnter = 14500;
    public const int ProbeComplete = 14505;
    public const int OperationEnter = 14510;
    public const int OperationComplete = 14515;
    public const int OperationFailed = 14520;
    public const int OperationWarning = 14525;
}
