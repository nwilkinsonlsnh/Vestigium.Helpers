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
    public const int HashComplete = 13025;
    public const int HashFileComplete = 13030;
    public const int HmacComplete = 13035;
    public const int KmacComplete = 13040;
    public const int ShakeComplete = 13045;
    public const int ChecksumComplete = 13050;
    public const int PasswordHashed = 13055;
    public const int PasswordVerify = 13060;
    public const int ConvertComplete = 13065;
    public const int Rejected = 13070;
}
