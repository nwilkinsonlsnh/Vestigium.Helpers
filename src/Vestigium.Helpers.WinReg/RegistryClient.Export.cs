using System.Text;
using Microsoft.Win32.SafeHandles;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.WinReg;

public sealed partial class RegistryClient
{
    public RegistryWriteResult Export(
        string path,
        RegistryHiveKind hive,
        string? key,
        RegistryExportFormat format = RegistryExportFormat.RegFile,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false,
        IProgress<RegistryCompareProgress>? progress = null,
        CancellationToken cancel = default)
    {
        var dest = HelperGuard.NotBlank(path, nameof(path));
        var keyPath = RegistryPath.Normalize(key);
        if (!confirm)
            return new RegistryWriteResult(RegistryWriteStatus.Denied, hive, keyPath, null, "confirm=false");
        if (cancel.IsCancellationRequested)
            return new RegistryWriteResult(RegistryWriteStatus.Denied, hive, keyPath, null, "canceled");

        using var opened = Open(hive, keyPath, view, writable: false);
        if (opened is null)
            return new RegistryWriteResult(RegistryWriteStatus.NotFound, hive, keyPath, null, "key gone");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(dest) is { Length: > 0 } dir ? dir : ".");
            progress?.Report(new RegistryCompareProgress { Phase = "Export", CurrentPath = keyPath });
            if (format == RegistryExportFormat.HiveFile)
                return ExportHive(opened.Handle, dest, hive, keyPath);

            using var writer = new StreamWriter(dest, false, Encoding.Unicode);
            RegistryRegFile.WriteTree(this, hive, keyPath, view, writer);
            if (cancel.IsCancellationRequested)
                return new RegistryWriteResult(RegistryWriteStatus.Denied, hive, keyPath, null, "canceled");
            HelperLog.Information(HelperLog.AppIds.WinReg, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"Export hive={hive} path={keyPath} file={dest}");
            progress?.Report(new RegistryCompareProgress { Phase = "Done", CurrentPath = keyPath });
            return new RegistryWriteResult(RegistryWriteStatus.Ok, hive, keyPath, null, dest);
        }
        catch (UnauthorizedAccessException ex)
        {
            return new RegistryWriteResult(RegistryWriteStatus.Denied, hive, keyPath, null, ex.Message);
        }
        catch (IOException ex)
        {
            return new RegistryWriteResult(RegistryWriteStatus.Denied, hive, keyPath, null, ex.Message);
        }
    }

    private static RegistryWriteResult ExportHive(SafeRegistryHandle handle, string dest, RegistryHiveKind hive, string keyPath)
    {
        using var privileges = RegistryNative.BackupRestore();
        if (File.Exists(dest))
            File.Delete(dest);
        var status = RegistryNative.RegSaveKeyEx(handle, dest, 0, RegistryNative.RegStandardFormat);
        if (status != 0)
            return new RegistryWriteResult(RegistryWriteStatus.Denied, hive, keyPath, null, "RegSaveKeyEx=" + status);
        HelperLog.Information(HelperLog.AppIds.WinReg, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"ExportHive hive={hive} path={keyPath} file={dest}");
        return new RegistryWriteResult(RegistryWriteStatus.Ok, hive, keyPath, null, dest);
    }
}
