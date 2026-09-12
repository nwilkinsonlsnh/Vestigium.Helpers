using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.WinReg;

public sealed partial class RegistryClient
{
    public RegistryWriteResult Import(
        string path,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false)
    {
        var file = HelperGuard.FileExists(path, nameof(path));
        if (!confirm)
            return new RegistryWriteResult(RegistryWriteStatus.Denied, RegistryHiveKind.CurrentUser, file, null, "confirm=false");

        var text = RegistryRegFile.ReadAllText(file);
        var lines = RegistryRegFile.PhysicalLines(text);
        if (lines.Count == 0)
            return new RegistryWriteResult(RegistryWriteStatus.InvalidPath, RegistryHiveKind.CurrentUser, file, null, "empty file");

        var header = lines[0].Trim();
        if (!header.StartsWith(RegistryRegFile.Header50, StringComparison.OrdinalIgnoreCase)
            && !header.Equals(RegistryRegFile.Header40, StringComparison.OrdinalIgnoreCase))
        {
            return new RegistryWriteResult(RegistryWriteStatus.InvalidPath, RegistryHiveKind.CurrentUser, file, null, "line 1 unknown header");
        }

        RegistryHiveKind? currentHive = null;
        var currentPath = string.Empty;
        var applied = 0;
        for (var i = 1; i < lines.Count; i++)
        {
            var line = lines[i].Trim();
            var lineNo = i + 1;
            if (line.Length == 0 || line.StartsWith(';'))
                continue;

            if (RegistryRegFile.TryParseKeyHeader(line, out var hive, out var keyPath, out var deleteKey))
            {
                currentHive = hive;
                currentPath = keyPath;
                var result = deleteKey
                    ? DeleteKey(hive, keyPath, recursive: true, view, confirm: true)
                    : CreateKey(hive, keyPath, view, confirm: true);
                if (result.Status is not RegistryWriteStatus.Ok and not RegistryWriteStatus.NotFound)
                    return FailLine(hive, keyPath, lineNo, result);
                applied++;
                continue;
            }

            if (currentHive is null)
                return new RegistryWriteResult(RegistryWriteStatus.InvalidPath, RegistryHiveKind.CurrentUser, file, null, $"line {lineNo} value before key");

            if (!RegistryRegFile.TryParseValue(line, out var name, out var deleteValue, out var kind, out var data, out var error))
                return new RegistryWriteResult(RegistryWriteStatus.InvalidPath, currentHive.Value, currentPath, name, $"line {lineNo} {error}");

            var write = deleteValue
                ? DeleteValue(currentHive.Value, currentPath, name, view, confirm: true)
                : SetValue(currentHive.Value, currentPath, name, data, kind, view, confirm: true);
            if (write.Status is not RegistryWriteStatus.Ok and not RegistryWriteStatus.NotFound)
                return FailLine(currentHive.Value, currentPath, lineNo, write);
            applied++;
        }

        HelperLog.Information(HelperLog.AppIds.WinReg, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"Import file={file} lines={applied}");
        return new RegistryWriteResult(RegistryWriteStatus.Ok, currentHive ?? RegistryHiveKind.CurrentUser, file, null, $"applied={applied}");
    }

    private static RegistryWriteResult FailLine(RegistryHiveKind hive, string keyPath, int lineNo, RegistryWriteResult inner)
        => new(inner.Status, hive, keyPath, inner.ValueName, $"line {lineNo} {inner.Reason}");
}
