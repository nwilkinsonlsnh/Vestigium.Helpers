namespace Vestigium.Helpers.Json;

/// <summary>Custom catalog block 13500–13999 (count by 5).</summary>
public static class JsonEvents
{
    public const int BlockStart = 13500;
    public const int BlockEnd = 13999;

    public const int ProbeEnter = 13500;
    public const int ProbeComplete = 13505;
    public const int SessionEnter = 13510;
    public const int SessionComplete = 13515;
    public const int SessionFailed = 13520;
    public const int DocumentEnter = 13525;
    public const int DocumentComplete = 13530;
    public const int DocumentFailed = 13535;
    public const int QueryEnter = 13540;
    public const int QueryComplete = 13545;
    public const int QueryFailed = 13550;
    public const int SnapshotEnter = 13555;
    public const int SnapshotComplete = 13560;
    public const int DiffEnter = 13565;
    public const int DiffComplete = 13570;
    public const int CommitEnter = 13575;
    public const int CommitComplete = 13580;
    public const int SaveEnter = 13585;
    public const int SaveComplete = 13590;
    public const int SaveWarning = 13595;
    public const int SaveFailed = 13600;
    public const int JsonlEnter = 13605;
    public const int JsonlComplete = 13610;
    public const int JsonlFailed = 13615;
    public const int GuardFailed = 13620;
    public const int OperationWarning = 13625;
}
