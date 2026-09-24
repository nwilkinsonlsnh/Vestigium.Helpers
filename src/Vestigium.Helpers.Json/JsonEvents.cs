namespace Vestigium.Helpers.Json;

/// <summary>Custom catalog block 13500–13999 (count by 5).</summary>
public static class JsonEvents
{
    /// <summary>First Event ID reserved for this package (inclusive).</summary>
    public const int BlockStart = 13500;
    /// <summary>Last Event ID reserved for this package (inclusive).</summary>
    public const int BlockEnd = 13999;

    /// <summary>Begin Probe.</summary>
    public const int ProbeEnter = 13500;
    /// <summary>Probe finished.</summary>
    public const int ProbeComplete = 13505;
    /// <summary>Begin a session operation.</summary>
    public const int SessionEnter = 13510;
    /// <summary>Session operation finished.</summary>
    public const int SessionComplete = 13515;
    /// <summary>Session operation failed.</summary>
    public const int SessionFailed = 13520;
    /// <summary>Begin a document parse or serialize.</summary>
    public const int DocumentEnter = 13525;
    /// <summary>Document parse or serialize finished.</summary>
    public const int DocumentComplete = 13530;
    /// <summary>Document parse or serialize failed.</summary>
    public const int DocumentFailed = 13535;
    /// <summary>Begin Set.</summary>
    public const int QueryEnter = 13540;
    /// <summary>Set finished.</summary>
    public const int QueryComplete = 13545;
    /// <summary>Set or path parse failed.</summary>
    public const int QueryFailed = 13550;
    /// <summary>Begin Snapshot.</summary>
    public const int SnapshotEnter = 13555;
    /// <summary>Snapshot finished.</summary>
    public const int SnapshotComplete = 13560;
    /// <summary>Begin Diff or Compare.</summary>
    public const int DiffEnter = 13565;
    /// <summary>Diff or Compare finished.</summary>
    public const int DiffComplete = 13570;
    /// <summary>Begin Commit, Revert, or Cancel.</summary>
    public const int CommitEnter = 13575;
    /// <summary>Commit, Revert, or Cancel finished.</summary>
    public const int CommitComplete = 13580;
    /// <summary>Begin Save, SaveAs, SaveWorking, or WriteFile.</summary>
    public const int SaveEnter = 13585;
    /// <summary>Save finished.</summary>
    public const int SaveComplete = 13590;
    /// <summary>SaveWorking wrote uncommitted working state.</summary>
    public const int SaveWarning = 13595;
    /// <summary>Save failed.</summary>
    public const int SaveFailed = 13600;
    /// <summary>Begin a JSONL operation.</summary>
    public const int JsonlEnter = 13605;
    /// <summary>JSONL operation finished.</summary>
    public const int JsonlComplete = 13610;
    /// <summary>JSONL operation failed.</summary>
    public const int JsonlFailed = 13615;
    /// <summary>A helper argument guard rejected the call.</summary>
    public const int GuardFailed = 13620;
    /// <summary>Non-fatal warning on an operation.</summary>
    public const int OperationWarning = 13625;
}
