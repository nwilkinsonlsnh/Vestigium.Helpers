using System.Text;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.WinReg;

internal static class RegistryIndexFromReg
{
    public static RegistryWriteResult Write(
        string regPath,
        string indexPath,
        bool confirm,
        bool includePayload,
        IProgress<RegistryCompareProgress>? progress,
        CancellationToken cancel)
    {
        indexPath = HelperGuard.NotBlank(indexPath, nameof(indexPath));
        if (!confirm)
            return new RegistryWriteResult(RegistryWriteStatus.Denied, RegistryHiveKind.CurrentUser, indexPath, null, "confirm=false");
        var source = HelperGuard.FileExists(regPath, nameof(regPath));
        if (cancel.IsCancellationRequested)
            return new RegistryWriteResult(RegistryWriteStatus.Denied, RegistryHiveKind.CurrentUser, indexPath, null, "canceled");

        var lines = RegistryRegFile.PhysicalLines(RegistryRegFile.ReadAllText(source));
        if (lines.Count == 0)
            return new RegistryWriteResult(RegistryWriteStatus.InvalidPath, RegistryHiveKind.CurrentUser, source, null, "empty file");
        var header = lines[0].Trim();
        if (!header.StartsWith(RegistryRegFile.Header50, StringComparison.OrdinalIgnoreCase)
            && !header.Equals(RegistryRegFile.Header40, StringComparison.OrdinalIgnoreCase))
            return new RegistryWriteResult(RegistryWriteStatus.InvalidPath, RegistryHiveKind.CurrentUser, source, null, "unknown header");

        RegistryHiveKind? hive = null;
        string? root = null;
        var keys = new List<string>();
        var values = new List<(string Path, RegistryValueInfo Value)>();

        var current = string.Empty;
        for (var i = 1; i < lines.Count; i++)
        {
            if (cancel.IsCancellationRequested)
                return new RegistryWriteResult(RegistryWriteStatus.Denied, hive ?? RegistryHiveKind.CurrentUser, indexPath, null, "canceled");
            var line = lines[i].Trim();
            if (line.Length == 0 || line.StartsWith(';'))
                continue;

            if (RegistryRegFile.TryParseKeyHeader(line, out var nextHive, out var keyPath, out var deleteKey))
            {
                if (deleteKey)
                    continue;
                if (hive is null)
                {
                    hive = nextHive;
                    root = keyPath;
                }
                else if (hive != nextHive)
                    return new RegistryWriteResult(RegistryWriteStatus.Denied, nextHive, keyPath, null, "multi-hive");
                current = keyPath;
                keys.Add(keyPath);
                continue;
            }

            if (hive is null)
                return new RegistryWriteResult(RegistryWriteStatus.InvalidPath, RegistryHiveKind.CurrentUser, source, null, $"line {i + 1} value before key");

            if (!RegistryRegFile.TryParseValue(line, out var name, out var deleteValue, out var kind, out var data, out var error))
                return new RegistryWriteResult(RegistryWriteStatus.InvalidPath, hive.Value, current, name, $"line {i + 1} {error}");
            if (deleteValue)
                continue;

            var info = new RegistryValueInfo
            {
                Name = name,
                IsDefault = name.Length == 0,
                Type = kind,
                Data = data,
                DataText = data as string
            };
            values.Add((current, info));
        }

        if (hive is null || root is null)
            return new RegistryWriteResult(RegistryWriteStatus.InvalidPath, RegistryHiveKind.CurrentUser, source, null, "no keys");

        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(indexPath)) is { Length: > 0 } dir ? dir : ".");
        using var writer = new StreamWriter(indexPath, false, new UTF8Encoding(false));
        WriteObj(writer, new Dictionary<string, object?>
        {
            ["rec"] = "header",
            ["schema"] = RegistryIndexWriter.Schema,
            ["machine"] = Environment.MachineName,
            ["capturedAt"] = DateTime.UtcNow.ToString("o"),
            ["hive"] = hive.Value.ToString(),
            ["path"] = root,
            ["view"] = RegistryViewKind.Default.ToString(),
            ["source"] = "reg"
        });

        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var keyCount = 0;
        foreach (var path in keys)
        {
            var relative = RegistryIndexWriter.Relative(root, path);
            if (!seen.Add(relative))
                continue;
            keyCount++;
            WriteObj(writer, new Dictionary<string, object?> { ["rec"] = "key", ["path"] = relative });
        }

        var valueCount = 0;
        foreach (var (path, value) in values)
        {
            valueCount++;
            var relative = RegistryIndexWriter.Relative(root, path);
            var row = new Dictionary<string, object?>
            {
                ["rec"] = "value",
                ["path"] = relative,
                ["name"] = value.Name,
                ["type"] = value.Type.ToString(),
                ["hash"] = RegistryIndexWriter.Hash(value)
            };
            if (includePayload)
            {
                var text = RegistryIndexWriter.SmallText(value);
                if (text is not null)
                    row["text"] = text;
            }
            WriteObj(writer, row);
        }

        WriteObj(writer, new Dictionary<string, object?> { ["rec"] = "footer", ["keys"] = keyCount, ["values"] = valueCount, ["stopped"] = false });
        HelperLog.Information(HelperLog.AppIds.WinReg, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"IndexFromReg keys={keyCount} values={valueCount}");
        progress?.Report(new RegistryCompareProgress { Phase = "Done", KeysSeen = keyCount, ValuesSeen = valueCount });
        return new RegistryWriteResult(RegistryWriteStatus.Ok, hive.Value, root, null, indexPath);
    }

    private static void WriteObj(StreamWriter writer, Dictionary<string, object?> map)
        => writer.WriteLine(System.Text.Json.JsonSerializer.Serialize(map));
}
