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
}
