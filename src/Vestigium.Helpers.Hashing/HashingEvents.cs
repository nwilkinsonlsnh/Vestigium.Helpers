namespace Vestigium.Helpers.Hashing;

/// <summary>Custom catalog block 13000–13499 (count by 5).</summary>
public static class HashingEvents
{
    public const int BlockStart = 13000;
    public const int BlockEnd = 13499;

    public const int ProbeEnter = 13000;
    public const int ProbeComplete = 13005;
    public const int OperationEnter = 13010;
    public const int OperationComplete = 13015;
    public const int OperationFailed = 13020;
}
