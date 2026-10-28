namespace Vestigium.Helpers.Kql.AppMgmt;

public sealed class PackageFacts
{
    public string? DisplayName { get; init; }
    public string? Publisher { get; init; }
    public string? Version { get; init; }
    public int? MajorVersion { get; init; }
    public int? MinorVersion { get; init; }
    public string? InstallDate { get; init; }
    public double? EstimatedSizeKb { get; init; }
    public bool IsX64 { get; init; }
    public bool IsGlobal { get; init; }
    public bool IsSystemComponent { get; init; }
    public bool WindowsInstaller { get; init; }
    public bool NoModify { get; init; }
    public bool NoRemove { get; init; }
    public bool NoRepair { get; init; }
    public bool HasIcon { get; init; }
    public bool HasModify { get; init; }
    public bool HasRepair { get; init; }
    public bool IsOrphan { get; init; }
    public string? BaselineStatus { get; init; }
    public bool PendingReboot { get; init; }
}
