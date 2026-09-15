namespace Vestigium.Helpers.WinReg;

public sealed partial class RegistryClient
{
    public RegistryWriteResult RenameValue(
        RegistryHiveKind hive,
        string? key,
        string from,
        string to,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false,
        RegistryJournal? journal = null)
    {
        var path = RegistryPath.Normalize(key);
        if (!confirm)
            return new RegistryWriteResult(RegistryWriteStatus.Denied, hive, path, from, "confirm=false");
        if (string.Equals(from, to, StringComparison.OrdinalIgnoreCase))
            return new RegistryWriteResult(RegistryWriteStatus.InvalidPath, hive, path, to, "same name");

        var existing = GetValue(hive, path, from, view);
        if (existing is null)
            return new RegistryWriteResult(RegistryWriteStatus.NotFound, hive, path, from, "value gone");
        if (GetValue(hive, path, to, view) is not null)
            return new RegistryWriteResult(RegistryWriteStatus.InUse, hive, path, to, "dest exists");

        var set = SetValue(hive, path, to, existing.Data, existing.Type, view, confirm: true, journal);
        if (set.Status != RegistryWriteStatus.Ok)
            return set;
        var deleted = DeleteValue(hive, path, from, view, confirm: true, journal);
        if (deleted.Status == RegistryWriteStatus.Ok)
            return new RegistryWriteResult(RegistryWriteStatus.Ok, hive, path, to, from);

        _ = DeleteValue(hive, path, to, view, confirm: true);
        return new RegistryWriteResult(RegistryWriteStatus.Denied, hive, path, from, deleted.Reason ?? "delete source failed");
    }
}
