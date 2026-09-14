namespace Vestigium.Helpers.WinReg;

public sealed partial class RegistryClient
{
    public RegistryWriteResult CopyKey(
        RegistryHiveKind sourceHive,
        string? sourceKey,
        RegistryHiveKind destHive,
        string? destKey,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false,
        IProgress<RegistryCompareProgress>? progress = null,
        CancellationToken cancel = default)
    {
        var src = RegistryPath.Normalize(sourceKey);
        var dest = RegistryPath.Normalize(destKey);
        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(dest))
            return new RegistryWriteResult(RegistryWriteStatus.InvalidPath, destHive, dest, null, "hive root is not copied");
        if (!confirm)
            return new RegistryWriteResult(RegistryWriteStatus.Denied, destHive, dest, null, "confirm=false");

        return CopyCore(sourceHive, src, destHive, dest, view, dest, progress, cancel, keys: 0);
    }

    private RegistryWriteResult CopyCore(
        RegistryHiveKind sourceHive,
        string src,
        RegistryHiveKind destHive,
        string dest,
        RegistryViewKind view,
        string rollbackRoot,
        IProgress<RegistryCompareProgress>? progress,
        CancellationToken cancel,
        int keys)
    {
        if (cancel.IsCancellationRequested)
            return Rollback(destHive, rollbackRoot, view, "canceled");

        var snap = GetKey(sourceHive, src, view, RegistryDetailLevel.Full);
        if (snap is null)
            return new RegistryWriteResult(RegistryWriteStatus.NotFound, sourceHive, src, null, "source gone");

        var created = CreateKey(destHive, dest, view, confirm: true);
        if (created.Status != RegistryWriteStatus.Ok)
            return created;

        keys++;
        progress?.Report(new RegistryCompareProgress { Phase = "Copy", KeysSeen = keys, CurrentPath = dest });

        foreach (var value in snap.Values)
        {
            if (cancel.IsCancellationRequested)
                return Rollback(destHive, rollbackRoot, view, "canceled");
            var set = SetValue(destHive, dest, value.Name, value.Data, value.Type, view, confirm: true);
            if (set.Status != RegistryWriteStatus.Ok)
                return Rollback(destHive, rollbackRoot, view, set.Reason ?? "set failed");
        }

        foreach (var child in snap.SubKeyNames)
        {
            if (cancel.IsCancellationRequested)
                return Rollback(destHive, rollbackRoot, view, "canceled");
            var copy = CopyCore(sourceHive, src + "\\" + child, destHive, dest + "\\" + child, view, rollbackRoot, progress, cancel, keys);
            if (copy.Status != RegistryWriteStatus.Ok)
                return copy;
        }

        return new RegistryWriteResult(RegistryWriteStatus.Ok, destHive, dest, null, src);
    }

    private RegistryWriteResult Rollback(RegistryHiveKind hive, string dest, RegistryViewKind view, string reason)
    {
        _ = DeleteKey(hive, dest, recursive: true, view, confirm: true);
        return new RegistryWriteResult(RegistryWriteStatus.Denied, hive, dest, null, reason);
    }

    public RegistryWriteResult RenameKey(
        RegistryHiveKind hive,
        string? sourceKey,
        string? destKey,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false,
        IProgress<RegistryCompareProgress>? progress = null,
        CancellationToken cancel = default)
    {
        var src = RegistryPath.Normalize(sourceKey);
        var dest = RegistryPath.Normalize(destKey);
        var copy = CopyKey(hive, src, hive, dest, view, confirm, progress, cancel);
        if (copy.Status != RegistryWriteStatus.Ok)
            return copy;

        var deleted = DeleteKey(hive, src, recursive: true, view, confirm: true);
        if (deleted.Status == RegistryWriteStatus.Ok)
            return new RegistryWriteResult(RegistryWriteStatus.Ok, hive, dest, null, src);

        _ = DeleteKey(hive, dest, recursive: true, view, confirm: true);
        return new RegistryWriteResult(RegistryWriteStatus.Denied, hive, src, null, deleted.Reason ?? "delete source failed");
    }
}
