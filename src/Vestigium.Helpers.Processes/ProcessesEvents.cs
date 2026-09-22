namespace Vestigium.Helpers.Processes;

/// <summary>Custom catalog block 15000–15499 (count by 5). Separate from ProcessGpuCatalog.</summary>
public static class ProcessesEvents
{
    public const int BlockStart = 15000;
    public const int BlockEnd = 15499;

    public const int ProbeEnter = 15000;
    public const int ProbeComplete = 15005;
    public const int OperationEnter = 15010;
    public const int OperationComplete = 15015;
    public const int OperationFailed = 15020;
    public const int OperationWarning = 15025;
}
