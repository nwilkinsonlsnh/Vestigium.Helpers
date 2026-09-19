namespace Vestigium.Helpers.Json;

/// <summary>Custom catalog block 13500–13999 (count by 5).</summary>
public static class JsonEvents
{
    public const int BlockStart = 13500;
    public const int BlockEnd = 13999;

    public const int ProbeEnter = 13500;
    public const int ProbeComplete = 13505;
    public const int OperationEnter = 13510;
    public const int OperationComplete = 13515;
    public const int OperationFailed = 13520;
    public const int OperationWarning = 13525;
}
