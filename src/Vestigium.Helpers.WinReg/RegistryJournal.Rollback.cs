using System.Text.Json;

namespace Vestigium.Helpers.WinReg;

public sealed partial class RegistryJournal
{
    public static RegistryWriteResult Rollback(
        string path,
        RegistryClient client,
        bool confirm,
        bool force = false)
    {
        path = Vestigium.Helpers.HelperGuard.NotBlank(path, nameof(path));
        if (!confirm)
            return new RegistryWriteResult(RegistryWriteStatus.Denied, RegistryHiveKind.CurrentUser, path, null, "confirm=false");
        if (!File.Exists(path))
            return new RegistryWriteResult(RegistryWriteStatus.NotFound, RegistryHiveKind.CurrentUser, path, null, "journal missing");

        var (muts, undone) = ReadMuts(path);
        var last = muts.Select(m => m.Batch).LastOrDefault();
        if (string.IsNullOrEmpty(last))
            return new RegistryWriteResult(RegistryWriteStatus.NotFound, RegistryHiveKind.CurrentUser, path, null, "no batch");

        var pending = muts.Where(m => m.Batch == last && !undone.Contains(m.Batch + ":" + m.Seq)).OrderByDescending(m => m.Seq).ToList();
        using var journal = Load(path, confirm: true, out var loaded);
        if (journal is null)
            return loaded;

        journal.BeginBatch("rollback", last);
        var restored = 0;
        var skipped = 0;
        foreach (var mut in pending)
        {
            var result = ApplyInverse(client, mut, force);
            if (result.Status == RegistryWriteStatus.Unsupported)
            {
                skipped++;
                journal.AppendUndo(mut, "skipped");
                continue;
            }
            if (result.Status != RegistryWriteStatus.Ok && result.Status != RegistryWriteStatus.NotFound)
            {
                journal.CommitBatch("failed");
                return result;
            }
            journal.AppendUndo(mut, "undone");
            restored++;
        }

        journal.CommitBatch();
        return new RegistryWriteResult(RegistryWriteStatus.Ok, RegistryHiveKind.CurrentUser, path, null, $"restored={restored} skipped={skipped}");
    }

    private void AppendUndo(JournalMut mut, string status)
        => AppendMut(new Dictionary<string, object?>
        {
            ["op"] = "mut-undo",
            ["targetBatch"] = mut.Batch,
            ["targetSeq"] = mut.Seq,
            ["status"] = status,
            ["hive"] = mut.Hive.ToString(),
            ["path"] = mut.Path
        });

    private static RegistryWriteResult ApplyInverse(RegistryClient client, JournalMut mut, bool force)
    {
        if (!force && mut.AfterHash is not null && mut.Name is not null && mut.Op is "SetValue" or "DeleteValue")
        {
            var live = client.GetValue(mut.Hive, mut.Path, mut.Name);
            if (live is not null && RegistryIndexWriter.Hash(live) != mut.AfterHash)
                return new RegistryWriteResult(RegistryWriteStatus.Unsupported, mut.Hive, mut.Path, mut.Name, "collision");
        }

        var dest = mut.To ?? mut.Name;
        return mut.Op switch
        {
            "SetValue" when mut.Existed && mut.Before is not null
                => client.SetValue(mut.Hive, mut.Path, mut.Name, mut.Before.Data, mut.Before.Type, confirm: true),
            "SetValue"
                => client.DeleteValue(mut.Hive, mut.Path, mut.Name, confirm: true),
            "DeleteValue" when mut.Before is not null
                => client.SetValue(mut.Hive, mut.Path, mut.Name, mut.Before.Data, mut.Before.Type, confirm: true),
            "CreateKey" when !mut.Existed
                => client.DeleteKey(mut.Hive, mut.Path, recursive: true, confirm: true),
            "DeleteKey"
                => new RegistryWriteResult(RegistryWriteStatus.Unsupported, mut.Hive, mut.Path, null, "no key snapshot"),
            "CopyKey" when dest is not null && !mut.Existed
                => client.DeleteKey(mut.Hive, dest, recursive: true, confirm: true),
            "RenameKey" when dest is not null
                => client.RenameKey(mut.Hive, dest, mut.Path, confirm: true),
            "SetSddl" when mut.Before?.DataText is { Length: > 0 } sddl
                => client.SetSddl(mut.Hive, mut.Path, sddl, confirm: true),
            "SetOwner" or "TakeOwnership" when mut.Name is { Length: > 0 } owner
                => client.SetOwner(mut.Hive, mut.Path, owner, confirm: true),
            "mut-undo"
                => new RegistryWriteResult(RegistryWriteStatus.Unsupported, mut.Hive, mut.Path, mut.Name, mut.Op),
            _ => new RegistryWriteResult(RegistryWriteStatus.Ok, mut.Hive, mut.Path, mut.Name, "noop")
        };
    }

    internal static (List<JournalMut> Muts, HashSet<string> Undone) ReadMuts(string path)
    {
        var muts = new List<JournalMut>();
        var undone = new HashSet<string>(StringComparer.Ordinal);
        foreach (var line in File.ReadLines(path))
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;
            using var doc = JsonDocument.Parse(line);
            if (doc.RootElement.GetProperty("rec").GetString() != "mut")
                continue;
            var op = doc.RootElement.TryGetProperty("op", out var opEl) ? opEl.GetString() ?? "" : "";
            if (op == "mut-undo")
            {
                var batch = doc.RootElement.TryGetProperty("targetBatch", out var b) ? b.GetString() ?? "" : "";
                var seq = doc.RootElement.TryGetProperty("targetSeq", out var s) ? s.GetInt32() : 0;
                undone.Add(batch + ":" + seq);
                continue;
            }

            muts.Add(JournalMut.Parse(doc.RootElement));
        }
        return (muts, undone);
    }

    internal sealed class JournalMut
    {
        public string Batch { get; init; } = "";
        public int Seq { get; init; }
        public string Op { get; init; } = "";
        public RegistryHiveKind Hive { get; init; }
        public string Path { get; init; } = "";
        public string? Name { get; init; }
        public string? To { get; init; }
        public bool Existed { get; init; }
        public string? AfterHash { get; init; }
        public RegistryValueInfo? Before { get; init; }

        public static JournalMut Parse(JsonElement el)
        {
            var typeName = el.TryGetProperty("beforeType", out var t) ? t.GetString() : null;
            var kind = Enum.TryParse<RegistryValueKind>(typeName, out var parsed) ? parsed : RegistryValueKind.String;
            var payload = el.TryGetProperty("beforePayload", out var p) && p.ValueKind == JsonValueKind.String ? p.GetString() : null;
            var valueName = el.TryGetProperty("name", out var nameEl) ? nameEl.GetString() : null;
            return new JournalMut
            {
                Batch = el.TryGetProperty("batch", out var b) ? b.GetString() ?? "" : "",
                Seq = el.TryGetProperty("seq", out var s) ? s.GetInt32() : 0,
                Op = el.TryGetProperty("op", out var o) ? o.GetString() ?? "" : "",
                Hive = Enum.TryParse<RegistryHiveKind>(el.GetProperty("hive").GetString(), out var hive) ? hive : RegistryHiveKind.CurrentUser,
                Path = el.TryGetProperty("path", out var path) ? path.GetString() ?? "" : "",
                Name = valueName,
                To = el.TryGetProperty("to", out var to) ? to.GetString() : null,
                Existed = el.TryGetProperty("existed", out var existed) && existed.ValueKind == JsonValueKind.True,
                AfterHash = el.TryGetProperty("afterHash", out var ah) ? ah.GetString() : null,
                Before = payload is null ? null : Unpack(kind, payload, valueName)
            };
        }

        private static RegistryValueInfo Unpack(RegistryValueKind kind, string payload, string? name)
        {
            object? data = kind switch
            {
                RegistryValueKind.DWord => int.TryParse(payload, out var d) ? d : 0,
                RegistryValueKind.QWord => long.TryParse(payload, out var q) ? q : 0L,
                RegistryValueKind.MultiString => payload.Split('\0'),
                RegistryValueKind.Binary or RegistryValueKind.None => Convert.FromBase64String(payload),
                _ => payload
            };
            return new RegistryValueInfo { Name = name ?? "", Type = kind, Data = data, DataText = payload };
        }
    }
}
