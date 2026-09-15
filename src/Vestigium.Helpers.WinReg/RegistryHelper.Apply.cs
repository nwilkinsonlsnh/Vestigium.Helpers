using System.Text.Json;

namespace Vestigium.Helpers.WinReg;

public static partial class RegistryHelper
{
    public static RegistryWriteResult Apply(
        string editsPath,
        string journalPath,
        bool confirm = false,
        bool protect = false)
        => Apply(RegistryEditList.Load(editsPath), journalPath, confirm, protect);

    public static RegistryWriteResult Apply(
        RegistryEditList edits,
        string journalPath,
        bool confirm = false,
        bool protect = false)
    {
        if (!confirm)
            return new RegistryWriteResult(RegistryWriteStatus.Denied, RegistryHiveKind.CurrentUser, journalPath, null, "confirm=false");

        var journal = CreateJournal(journalPath, confirm: true, protect, out var created);
        if (journal is null)
            return created;

        try
        {
            journal.EnsureBatch("edits", edits.Label);
            foreach (var op in edits.Ops)
            {
                var result = Run(op, journal);
                if (result.Status != RegistryWriteStatus.Ok)
                    return result;
            }

            journal.CommitBatch();
            return new RegistryWriteResult(RegistryWriteStatus.Ok, RegistryHiveKind.CurrentUser, journalPath, null, $"ops={edits.Ops.Count}");
        }
        finally
        {
            journal.Dispose();
        }
    }

    private static RegistryWriteResult Run(RegistryEditOp op, RegistryJournal journal)
    {
        return op.Op switch
        {
            "CreateKey" => Local.CreateKey(op.Hive, op.Path, op.View, confirm: true, journal),
            "DeleteKey" => Local.DeleteKey(op.Hive, op.Path, op.Recursive, op.View, confirm: true, journal),
            "SetValue" => Local.SetValue(op.Hive, op.Path, op.Name, Unpack(op), op.Type, op.View, confirm: true, journal),
            "DeleteValue" => Local.DeleteValue(op.Hive, op.Path, op.Name, op.View, confirm: true, journal),
            "RenameValue" => Local.RenameValue(op.Hive, op.Path, op.From ?? "", op.To ?? "", op.View, confirm: true, journal),
            "RenameKey" => Local.RenameKey(op.Hive, op.Path, op.To, op.View, confirm: true),
            _ => new RegistryWriteResult(RegistryWriteStatus.InvalidPath, op.Hive, op.Path, op.Name, "unknown op " + op.Op)
        };
    }

    private static object? Unpack(RegistryEditOp op)
    {
        if (op.Data is null)
            return op.Type is RegistryValueKind.Binary or RegistryValueKind.None ? Array.Empty<byte>() : "";
        var el = op.Data.Value;
        return op.Type switch
        {
            RegistryValueKind.DWord => el.ValueKind == JsonValueKind.Number ? el.GetInt32() : int.Parse(el.GetString() ?? "0"),
            RegistryValueKind.QWord => el.ValueKind == JsonValueKind.Number ? el.GetInt64() : long.Parse(el.GetString() ?? "0"),
            RegistryValueKind.MultiString => el.EnumerateArray().Select(e => e.GetString() ?? "").ToArray(),
            RegistryValueKind.Binary or RegistryValueKind.None => el.ValueKind == JsonValueKind.String ? Convert.FromBase64String(el.GetString() ?? "") : [],
            _ => el.GetString() ?? el.GetRawText().Trim('"')
        };
    }
}
