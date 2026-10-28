using Vestigium.Helpers;

namespace Vestigium.Helpers.WinReg;

public sealed partial class RegistryClient
{
    public RegistryWriteResult SetOwner(
        RegistryHiveKind hive,
        string? key,
        string account,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false,
        RegistryJournal? journal = null)
    {
        _ = view;
        var path = RegistryPath.Normalize(key);
        if (!confirm)
            return Fail(hive, path, null, RegistryWriteStatus.Denied, "confirm=false");
        if (IsForbidden(hive, path))
            return Fail(hive, path, null, RegistryWriteStatus.Denied, "forbidden key");
        if (!RegistryAcl.TryResolveAccount(HelperGuard.NotBlank(account, nameof(account)), out var sid, out var resolve))
            return Fail(hive, path, null, RegistryWriteStatus.InvalidPath, resolve ?? "account");
        var before = GetKey(hive, path, view, RegistryDetailLevel.Full);
        var result = ApplyOwner(hive, path, sid);
        if (result.Status == RegistryWriteStatus.Ok)
            journal?.RecordAcl("SetOwner", hive, path, before?.Owner, before?.Sddl);
        return result;
    }

    public RegistryWriteResult TakeOwnership(
        RegistryHiveKind hive,
        string? key,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false,
        RegistryJournal? journal = null)
    {
        _ = view;
        var path = RegistryPath.Normalize(key);
        if (!confirm)
            return Fail(hive, path, null, RegistryWriteStatus.Denied, "confirm=false");
        if (IsForbidden(hive, path))
            return Fail(hive, path, null, RegistryWriteStatus.Denied, "forbidden key");
        var before = GetKey(hive, path, view, RegistryDetailLevel.Full);
        var result = ApplyOwner(hive, path, RegistryAcl.CurrentUser());
        if (result.Status == RegistryWriteStatus.Ok)
            journal?.RecordAcl("TakeOwnership", hive, path, before?.Owner, before?.Sddl);
        return result;
    }

    public RegistryWriteResult SetSddl(
        RegistryHiveKind hive,
        string? key,
        string sddl,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false,
        RegistryJournal? journal = null)
    {
        _ = view;
        var path = RegistryPath.Normalize(key);
        sddl = HelperGuard.NotBlank(sddl, nameof(sddl));
        if (!confirm)
            return Fail(hive, path, null, RegistryWriteStatus.Denied, "confirm=false");
        if (IsForbidden(hive, path))
            return Fail(hive, path, null, RegistryWriteStatus.Denied, "forbidden key");
        if (!sddl.Contains("D:", StringComparison.OrdinalIgnoreCase))
            return Fail(hive, path, null, RegistryWriteStatus.InvalidPath, "SDDL missing DACL");

        var before = GetKey(hive, path, view, RegistryDetailLevel.Full);
        using var handle = RegistryAcl.OpenWriteAcl(hive, path, out var open);
        if (handle is null)
            return Fail(hive, path, null, open == 2 ? RegistryWriteStatus.NotFound : RegistryWriteStatus.Denied, "RegOpenKeyEx=" + open);
        var status = RegistryAcl.TrySetSddl(handle, sddl);
        if (status != 0)
            return Fail(hive, path, null, RegistryWriteStatus.Denied, "SetSecurityInfo=" + status);
        journal?.RecordAcl("SetSddl", hive, path, before?.Owner, before?.Sddl);
        Log("SetSddl", hive, path, null);
        return Ok(hive, path, null);
    }

    private RegistryWriteResult ApplyOwner(RegistryHiveKind hive, string path, System.Security.Principal.SecurityIdentifier sid)
    {
        using var handle = RegistryAcl.OpenWriteAcl(hive, path, out var open);
        if (handle is null)
            return Fail(hive, path, null, open == 2 ? RegistryWriteStatus.NotFound : RegistryWriteStatus.Denied, "RegOpenKeyEx=" + open);
        var status = RegistryAcl.TrySetOwner(handle, sid);
        if (status != 0)
            return Fail(hive, path, null, RegistryWriteStatus.Denied, "SetSecurityInfo=" + status);
        Log("SetOwner", hive, path, null);
        return Ok(hive, path, null);
    }
}
