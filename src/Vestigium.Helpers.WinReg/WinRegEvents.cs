namespace Vestigium.Helpers.WinReg;

/// <summary>Custom catalog block 16000–16499 (count by 5).</summary>
public static class WinRegEvents
{
    public const int BlockStart = 16000;
    public const int BlockEnd = 16499;

    public const int ProbeEnter = 16000;
    public const int ProbeComplete = 16005;
    public const int OperationEnter = 16010;
    public const int OperationComplete = 16015;
    public const int OperationFailed = 16020;
    public const int OperationWarning = 16025;
}
