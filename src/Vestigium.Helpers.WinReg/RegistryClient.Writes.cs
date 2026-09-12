using Microsoft.Win32;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.WinReg;

public sealed partial class RegistryClient
{
    public RegistryWriteResult CreateKey(
        RegistryHiveKind hive,
        string? key,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false)
    {
        var path = RegistryPath.Normalize(key);
        if (string.IsNullOrEmpty(path))
            return Fail(hive, path, null, RegistryWriteStatus.InvalidPath, "hive root is not created");
        if (!confirm)
            return Fail(hive, path, null, RegistryWriteStatus.Denied, "confirm=false");
        if (IsForbidden(hive, path))
            return Fail(hive, path, null, RegistryWriteStatus.Denied, "forbidden key");

        try
        {
            using var created = OpenOrCreate(hive, path, view);
            if (created is null)
                return Fail(hive, path, null, RegistryWriteStatus.Denied, "CreateSubKey");
            Log("CreateKey", hive, path, null);
            return Ok(hive, path, null);
        }
        catch (UnauthorizedAccessException ex) { return Fail(hive, path, null, RegistryWriteStatus.Denied, ex.Message); }
        catch (IOException ex) { return Fail(hive, path, null, RegistryWriteStatus.Denied, ex.Message); }
    }

    public RegistryWriteResult DeleteKey(
        RegistryHiveKind hive,
        string? key,
        bool recursive = false,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false)
    {
        var path = RegistryPath.Normalize(key);
        if (string.IsNullOrEmpty(path))
            return Fail(hive, path, null, RegistryWriteStatus.InvalidPath, "hive root is not deleted");
        if (!confirm)
            return Fail(hive, path, null, RegistryWriteStatus.Denied, "confirm=false");
        if (IsForbidden(hive, path))
            return Fail(hive, path, null, RegistryWriteStatus.Denied, "forbidden key");

        try
        {
            using var existing = Open(hive, path, view, writable: true);
            if (existing is null)
                return Fail(hive, path, null, RegistryWriteStatus.NotFound, "key gone");
            if (!recursive && existing.SubKeyCount > 0)
                return Fail(hive, path, null, RegistryWriteStatus.InvalidPath, "key has subkeys");
        }
        catch (UnauthorizedAccessException ex) { return Fail(hive, path, null, RegistryWriteStatus.Denied, ex.Message); }

        try
        {
            using var parent = OpenParent(hive, path, view, writable: true);
            if (parent is null)
                return Fail(hive, path, null, RegistryWriteStatus.Denied, "OpenParent");
            var leaf = RegistryPath.Leaf(path);
            if (recursive) parent.DeleteSubKeyTree(leaf, throwOnMissingSubKey: false);
            else parent.DeleteSubKey(leaf, throwOnMissingSubKey: false);
            Log("DeleteKey", hive, path, null);
            return Ok(hive, path, null);
        }
        catch (UnauthorizedAccessException ex) { return Fail(hive, path, null, RegistryWriteStatus.Denied, ex.Message); }
        catch (IOException ex) { return Fail(hive, path, null, RegistryWriteStatus.Denied, ex.Message); }
    }

    public RegistryWriteResult SetValue(
        RegistryHiveKind hive,
        string? key,
        string? valueName,
        object? data,
        RegistryValueKind kind = RegistryValueKind.String,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false)
    {
        var path = RegistryPath.Normalize(key);
        var name = valueName ?? string.Empty;
        if (string.IsNullOrEmpty(path))
            return Fail(hive, path, name, RegistryWriteStatus.InvalidPath, "hive root is not written");
        if (!confirm)
            return Fail(hive, path, name, RegistryWriteStatus.Denied, "confirm=false");
        if (IsForbidden(hive, path))
            return Fail(hive, path, name, RegistryWriteStatus.Denied, "forbidden key");
        if (!TypesMatch(kind, data))
            return Fail(hive, path, name, RegistryWriteStatus.TypeMismatch, $"{kind} vs {data?.GetType().Name ?? "null"}");

        try
        {
            using var opened = OpenOrCreate(hive, path, view);
            if (opened is null)
                return Fail(hive, path, name, RegistryWriteStatus.Denied, "CreateSubKey");
            opened.SetValue(name, Coerce(kind, data)!, MapKind(kind));
            Log("SetValue", hive, path, name);
            return Ok(hive, path, name);
        }
        catch (UnauthorizedAccessException ex) { return Fail(hive, path, name, RegistryWriteStatus.Denied, ex.Message); }
        catch (IOException ex) { return Fail(hive, path, name, RegistryWriteStatus.Denied, ex.Message); }
    }

    public RegistryWriteResult DeleteValue(
        RegistryHiveKind hive,
        string? key,
        string? valueName,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false)
    {
        var path = RegistryPath.Normalize(key);
        var name = valueName ?? string.Empty;
        if (!confirm)
            return Fail(hive, path, name, RegistryWriteStatus.Denied, "confirm=false");
        if (IsForbidden(hive, path))
            return Fail(hive, path, name, RegistryWriteStatus.Denied, "forbidden key");

        try
        {
            using var opened = Open(hive, path, view, writable: true);
            if (opened is null)
                return Fail(hive, path, name, RegistryWriteStatus.NotFound, "key gone");
            opened.DeleteValue(name, throwIfMissing: false);
            Log("DeleteValue", hive, path, name);
            return Ok(hive, path, name);
        }
        catch (UnauthorizedAccessException ex) { return Fail(hive, path, name, RegistryWriteStatus.Denied, ex.Message); }
        catch (IOException ex) { return Fail(hive, path, name, RegistryWriteStatus.Denied, ex.Message); }
    }

    private RegistryKey? OpenOrCreate(RegistryHiveKind hive, string path, RegistryViewKind view)
    {
        RegistryKey? root = null;
        try
        {
            root = Machine is null
                ? RegistryKey.OpenBaseKey(MapHive(hive), MapView(view))
                : RegistryKey.OpenRemoteBaseKey(MapHive(hive), Machine, MapView(view));
            var created = root.CreateSubKey(path, writable: true);
            root.Dispose();
            return created;
        }
        catch
        {
            root?.Dispose();
            return null;
        }
    }

    private RegistryKey? OpenParent(RegistryHiveKind hive, string path, RegistryViewKind view, bool writable)
    {
        var i = path.LastIndexOf('\\');
        var parent = i < 0 ? string.Empty : path[..i];
        return Open(hive, parent, view, writable);
    }

    private static bool IsForbidden(RegistryHiveKind hive, string path)
    {
        if (hive != RegistryHiveKind.LocalMachine)
            return false;
        var p = path.ToUpperInvariant();
        return p is "SYSTEM" or "SAM" or "SECURITY"
            || p.StartsWith("SYSTEM\\", StringComparison.Ordinal)
            || p.StartsWith("SAM\\", StringComparison.Ordinal)
            || p.StartsWith("SECURITY\\", StringComparison.Ordinal)
            || p == "SOFTWARE\\MICROSOFT"
            || p.StartsWith("SOFTWARE\\MICROSOFT\\", StringComparison.Ordinal);
    }

    private static bool TypesMatch(RegistryValueKind kind, object? data) => kind switch
    {
        RegistryValueKind.None => data is null or byte[],
        RegistryValueKind.String or RegistryValueKind.ExpandString => data is string,
        RegistryValueKind.DWord => data is int or uint,
        RegistryValueKind.QWord => data is long or ulong or int,
        RegistryValueKind.MultiString => data is string[],
        RegistryValueKind.Binary => data is byte[],
        _ => false
    };

    private static object? Coerce(RegistryValueKind kind, object? data) => kind switch
    {
        RegistryValueKind.DWord when data is uint u => unchecked((int)u),
        RegistryValueKind.QWord when data is int i => (long)i,
        RegistryValueKind.QWord when data is ulong u => unchecked((long)u),
        RegistryValueKind.None => data ?? Array.Empty<byte>(),
        _ => data
    };

    private static Microsoft.Win32.RegistryValueKind MapKind(RegistryValueKind kind) => kind switch
    {
        RegistryValueKind.String => Microsoft.Win32.RegistryValueKind.String,
        RegistryValueKind.ExpandString => Microsoft.Win32.RegistryValueKind.ExpandString,
        RegistryValueKind.Binary => Microsoft.Win32.RegistryValueKind.Binary,
        RegistryValueKind.DWord => Microsoft.Win32.RegistryValueKind.DWord,
        RegistryValueKind.MultiString => Microsoft.Win32.RegistryValueKind.MultiString,
        RegistryValueKind.QWord => Microsoft.Win32.RegistryValueKind.QWord,
        _ => Microsoft.Win32.RegistryValueKind.None
    };

    private static RegistryWriteResult Ok(RegistryHiveKind hive, string path, string? name)
        => new(RegistryWriteStatus.Ok, hive, path, name, null);

    private static RegistryWriteResult Fail(RegistryHiveKind hive, string path, string? name, RegistryWriteStatus status, string reason)
    {
        HelperLog.Error(HelperLog.AppIds.WinReg, VestigiumStatus.Failed, HelperLog.Subcategories.Inventory, $"{status} hive={hive} path={path} name={name} {reason}");
        return new RegistryWriteResult(status, hive, path, name, reason);
    }

    private static void Log(string verb, RegistryHiveKind hive, string path, string? name)
        => HelperLog.Information(HelperLog.AppIds.WinReg, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"{verb} hive={hive} path={path} name={name}");
}
