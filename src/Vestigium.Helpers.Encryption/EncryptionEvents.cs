namespace Vestigium.Helpers.Encryption;

/// <summary>Custom catalog block 12000–12499 (count by 5).</summary>
public static class EncryptionEvents
{
    public const int BlockStart = 12000;
    public const int BlockEnd = 12499;

    public const int ProbeEnter = 12000;
    public const int ProbeComplete = 12005;
    public const int OperationEnter = 12010;
    public const int OperationComplete = 12015;
    public const int OperationFailed = 12020;
    public const int TokenWarning = 12025;
    public const int FileRejected = 12030;
}
