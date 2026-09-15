namespace Vestigium.Helpers.WinReg;

public sealed partial class RegistryClient
{
    public RegistryWriteResult CopyKey(
        RegistryHiveKind sourceHive,
        string? sourceKey,
        RegistryHiveKind destHive,
        string? destKey,
        RegistryViewKind view,
        bool confirm,
        IProgress<RegistryCompareProgress>? progress,
        CancellationToken cancel,
        RegistryJournal? journal)
    {
        var dest = RegistryPath.Normalize(destKey);
        var existed = GetKey(destHive, dest, view, RegistryDetailLevel.Identity) is not null;
        var result = CopyKey(sourceHive, sourceKey, destHive, destKey, view, confirm, progress, cancel);
        if (result.Status == RegistryWriteStatus.Ok)
            journal?.RecordMove("CopyKey", destHive, RegistryPath.Normalize(sourceKey), dest, existed);
        return result;
    }

    public RegistryWriteResult RenameKey(
        RegistryHiveKind hive,
        string? sourceKey,
        string? destKey,
        RegistryViewKind view,
        bool confirm,
        IProgress<RegistryCompareProgress>? progress,
        CancellationToken cancel,
        RegistryJournal journal)
    {
        var result = RenameKey(hive, sourceKey, destKey, view, confirm, progress, cancel);
        if (result.Status == RegistryWriteStatus.Ok)
            journal.RecordMove("RenameKey", hive, RegistryPath.Normalize(sourceKey), RegistryPath.Normalize(destKey), destExisted: false);
        return result;
    }
}
