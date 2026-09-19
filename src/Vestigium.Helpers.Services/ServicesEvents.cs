namespace Vestigium.Helpers.Services;

/// <summary>Custom catalog block 15500–15999 (count by 5).</summary>
public static class ServicesEvents
{
    public const int BlockStart = 15500;
    public const int BlockEnd = 15999;

    public const int ProbeEnter = 15500;
    public const int ProbeComplete = 15505;
    public const int OperationEnter = 15510;
    public const int OperationComplete = 15515;
    public const int OperationFailed = 15520;
    public const int OperationWarning = 15525;
}
