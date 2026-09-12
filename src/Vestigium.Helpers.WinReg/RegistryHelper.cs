using System.Runtime.Versioning;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.WinReg;

/// <summary>Windows Registry helpers. Windows-only.</summary>
[SupportedOSPlatform("windows")]
public static class RegistryHelper
{
    public static string Identity => "Vestigium.Helpers.WinReg";

    public static TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(3);

    public static RegistryClient Local { get; } = new(null);

    public static RegistryClient For(string machine)
        => new(HelperGuard.NotBlank(machine, nameof(machine)));

    public static bool CanConnect(string machine)
        => CanConnect(machine, out _);

    public static bool CanConnect(string machine, out string? reason)
    {
        var client = For(machine);
        if (client.IsLocal)
        {
            reason = null;
            return true;
        }

        try
        {
            var timeout = ConnectTimeout <= TimeSpan.Zero ? TimeSpan.FromSeconds(3) : ConnectTimeout;
            var task = Task.Run(() => client.Open(
                RegistryHiveKind.LocalMachine,
                string.Empty,
                RegistryViewKind.Default,
                writable: false));
            if (!task.Wait(timeout))
            {
                reason = "timeout";
                return false;
            }

            using var key = task.Result;
            if (key is null)
            {
                reason = "OpenRemoteBaseKey failed";
                return false;
            }

            reason = null;
            return true;
        }
        catch (Exception ex)
        {
            reason = ex.GetType().Name;
            return false;
        }
    }

    public static RegistryWriteResult Export(
        string path,
        RegistryHiveKind hive,
        string? key,
        RegistryExportFormat format = RegistryExportFormat.RegFile,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false)
        => Local.Export(path, hive, key, format, view, confirm);

    public static RegistryWriteResult Import(
        string path,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false)
        => Local.Import(path, view, confirm);

    public static IReadOnlyList<RegistryHit> Search(
        RegistryHiveKind hive,
        string? key,
        string term,
        RegistrySearchMode mode = RegistrySearchMode.Contains,
        RegistrySearchFields fields = RegistrySearchFields.KeyName | RegistrySearchFields.ValueName,
        int maxDepth = 16,
        int maxResults = RegistryClient.MaxSearchResults,
        RegistryViewKind view = RegistryViewKind.Default)
        => Local.Search(hive, key, term, mode, fields, maxDepth, maxResults, view);

    public static IRegistryMount? MountHive(
        string hiveFile,
        RegistryHiveKind destination,
        string subKey,
        bool confirm,
        out RegistryWriteResult result)
    {
        var file = HelperGuard.NotBlank(hiveFile, nameof(hiveFile));
        var name = RegistryPath.Normalize(HelperGuard.NotBlank(subKey, nameof(subKey)));
        if (destination is not RegistryHiveKind.LocalMachine and not RegistryHiveKind.Users)
        {
            result = new RegistryWriteResult(RegistryWriteStatus.Unsupported, destination, name, null, "LocalMachine or Users only");
            return null;
        }

        if (!confirm)
        {
            result = new RegistryWriteResult(RegistryWriteStatus.Denied, destination, name, null, "confirm=false");
            return null;
        }

        if (!File.Exists(file))
        {
            result = new RegistryWriteResult(RegistryWriteStatus.NotFound, destination, name, null, "hive file missing");
            return null;
        }

        if (Local.GetKey(destination, name) is not null)
        {
            result = new RegistryWriteResult(RegistryWriteStatus.InUse, destination, name, null, "subkey already present");
            return null;
        }

        _ = RegistryNative.EnablePrivileges("SeBackupPrivilege", "SeRestorePrivilege");
        var status = RegistryNative.RegLoadKey(RegistryMount.HiveHandle(destination), name, file);
        if (status != 0)
        {
            result = new RegistryWriteResult(RegistryWriteStatus.Denied, destination, name, null, "RegLoadKey=" + status);
            return null;
        }

        HelperLog.Information(HelperLog.AppIds.WinReg, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"Mount dest={destination} sub={name}");
        result = new RegistryWriteResult(RegistryWriteStatus.Ok, destination, name, null, file);
        return new RegistryMount(file, destination, name);
    }

    public static RegistryWriteResult DismountHive(
        RegistryHiveKind destination,
        string subKey,
        bool confirm = false)
    {
        var name = RegistryPath.Normalize(HelperGuard.NotBlank(subKey, nameof(subKey)));
        if (destination is not RegistryHiveKind.LocalMachine and not RegistryHiveKind.Users)
            return new RegistryWriteResult(RegistryWriteStatus.Unsupported, destination, name, null, "LocalMachine or Users only");
        if (!confirm)
            return new RegistryWriteResult(RegistryWriteStatus.Denied, destination, name, null, "confirm=false");

        _ = RegistryNative.EnablePrivileges("SeBackupPrivilege", "SeRestorePrivilege");
        var status = RegistryNative.RegUnLoadKey(RegistryMount.HiveHandle(destination), name);
        if (status != 0)
            return new RegistryWriteResult(RegistryWriteStatus.Denied, destination, name, null, "RegUnLoadKey=" + status);

        HelperLog.Information(HelperLog.AppIds.WinReg, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"Dismount dest={destination} sub={name}");
        return new RegistryWriteResult(RegistryWriteStatus.Ok, destination, name, null, null);
    }

    public static string Probe()
    {
        var app = HelperLog.AppIds.WinReg;
        HelperLog.Information(app, VestigiumStatus.Pending, HelperLog.Subcategories.Inventory, "Opening HKCU\\Software.");
        _ = Local.GetKey(RegistryHiveKind.CurrentUser, "Software", RegistryViewKind.Default, RegistryDetailLevel.Identity);
        HelperLog.Information(app, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, "Registry probe complete. Identity=" + Identity);
        return Identity;
    }
}
