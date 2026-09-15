namespace Vestigium.Helpers.WinReg;

public sealed partial class RegistryJournal
{
    public const int MaxPayloadBytes = 64 * 1024;

    public RegistryWriteResult EnsureBatch(string kind = "crud")
        => _openBatch is null ? BeginBatch(kind) : new RegistryWriteResult(RegistryWriteStatus.Ok, RegistryHiveKind.CurrentUser, Path, null, _openBatch);

    public RegistryWriteResult RecordValue(
        string op,
        RegistryHiveKind hive,
        string path,
        string? name,
        RegistryValueInfo? before,
        RegistryValueInfo? after)
    {
        _ = EnsureBatch();
        var row = Base(op, hive, path, name);
        row["existed"] = before is not null;
        PutPayload(row, "before", before);
        PutPayload(row, "after", after);
        return AppendMut(row);
    }

    public RegistryWriteResult RecordKey(
        string op,
        RegistryHiveKind hive,
        string path,
        bool existed)
    {
        _ = EnsureBatch();
        var row = Base(op, hive, path, null);
        row["existed"] = existed;
        return AppendMut(row);
    }

    public RegistryWriteResult RecordMove(
        string op,
        RegistryHiveKind hive,
        string source,
        string dest,
        bool destExisted)
    {
        _ = EnsureBatch();
        var row = Base(op, hive, source, dest);
        row["to"] = dest;
        row["existed"] = destExisted;
        return AppendMut(row);
    }

    public RegistryWriteResult RecordAcl(
        string op,
        RegistryHiveKind hive,
        string path,
        string? owner,
        string? sddl)
    {
        _ = EnsureBatch();
        var row = Base(op, hive, path, owner);
        if (sddl is not null && System.Text.Encoding.UTF8.GetByteCount(sddl) <= MaxPayloadBytes)
            row["beforePayload"] = sddl;
        else if (sddl is not null)
            row["beforeOmitted"] = true;
        return AppendMut(row);
    }

    private static Dictionary<string, object?> Base(string op, RegistryHiveKind hive, string path, string? name)
        => new()
        {
            ["op"] = op,
            ["hive"] = hive.ToString(),
            ["path"] = path,
            ["name"] = name
        };

    private static void PutPayload(Dictionary<string, object?> row, string prefix, RegistryValueInfo? value)
    {
        if (value is null)
            return;
        row[prefix + "Type"] = value.Type.ToString();
        row[prefix + "Hash"] = RegistryIndexWriter.Hash(value);
        var packed = Pack(value);
        if (packed is null)
        {
            row[prefix + "Omitted"] = true;
            return;
        }
        row[prefix + "Payload"] = packed;
    }

    private static string? Pack(RegistryValueInfo value)
    {
        var text = value.Type switch
        {
            RegistryValueKind.String or RegistryValueKind.ExpandString => value.Data as string ?? value.DataText,
            RegistryValueKind.DWord or RegistryValueKind.QWord => value.DataText,
            RegistryValueKind.MultiString when value.Data is string[] parts => string.Join("\u0000", parts),
            RegistryValueKind.Binary or RegistryValueKind.None when value.Data is byte[] bytes => Convert.ToBase64String(bytes),
            _ => value.DataText
        };
        if (text is null)
            return null;
        if (System.Text.Encoding.UTF8.GetByteCount(text) > MaxPayloadBytes)
            return null;
        return text;
    }
}
