namespace Vestigium.Helpers.WinReg;

public sealed partial class RegistryClient
{
    public RegistryWriteResult CopyKey(
        RegistryHiveKind sourceHive,
        string? sourceKey,
        RegistryHiveKind destHive,
        string? destKey,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false)
    {
        var src = RegistryPath.Normalize(sourceKey);
        var dest = RegistryPath.Normalize(destKey);
        if (string.IsNullOrEmpty(src) || string.IsNullOrEmpty(dest))
            return new RegistryWriteResult(RegistryWriteStatus.InvalidPath, destHive, dest, null, "hive root is not copied");
        if (!confirm)
            return new RegistryWriteResult(RegistryWriteStatus.Denied, destHive, dest, null, "confirm=false");

        var snap = GetKey(sourceHive, src, view, RegistryDetailLevel.Full);
        if (snap is null)
            return new RegistryWriteResult(RegistryWriteStatus.NotFound, sourceHive, src, null, "source gone");

        var created = CreateKey(destHive, dest, view, confirm: true);
        if (created.Status != RegistryWriteStatus.Ok)
            return created;

        foreach (var value in snap.Values)
        {
            var set = SetValue(destHive, dest, value.Name, value.Data, value.Type, view, confirm: true);
            if (set.Status != RegistryWriteStatus.Ok)
                return set;
        }

        foreach (var child in snap.SubKeyNames)
        {
            var nextSrc = src + "\\" + child;
            var nextDest = dest + "\\" + child;
            var copy = CopyKey(sourceHive, nextSrc, destHive, nextDest, view, confirm: true);
            if (copy.Status != RegistryWriteStatus.Ok)
                return copy;
        }

        return new RegistryWriteResult(RegistryWriteStatus.Ok, destHive, dest, null, src);
    }

    public RegistryWriteResult RenameKey(
        RegistryHiveKind hive,
        string? sourceKey,
        string? destKey,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false)
    {
        var copy = CopyKey(hive, sourceKey, hive, destKey, view, confirm);
        if (copy.Status != RegistryWriteStatus.Ok)
            return copy;
        var deleted = DeleteKey(hive, sourceKey, recursive: true, view, confirm: true);
        return deleted.Status == RegistryWriteStatus.Ok
            ? new RegistryWriteResult(RegistryWriteStatus.Ok, hive, RegistryPath.Normalize(destKey), null, RegistryPath.Normalize(sourceKey))
            : deleted;
    }
}
