using System.Text.Json;

namespace Vestigium.Helpers.WinReg;

public sealed partial class RegistryClient
{
    public const int MaxSnapshotKeys = 256;

    internal string? SnapshotTree(RegistryHiveKind hive, string path, RegistryViewKind view)
    {
        var nodes = new List<Dictionary<string, object?>>();
        return !Walk(hive, path, view, nodes) ? null : JsonSerializer.Serialize(nodes);
    }

    internal RegistryWriteResult RestoreTree(string json, RegistryHiveKind hive, RegistryViewKind view = RegistryViewKind.Default)
    {
        using var doc = JsonDocument.Parse(json);
        foreach (var node in doc.RootElement.EnumerateArray())
        {
            var path = node.GetProperty("p").GetString() ?? "";
            var created = CreateKey(hive, path, view, confirm: true);
            if (created.Status != RegistryWriteStatus.Ok && created.Status != RegistryWriteStatus.InUse)
                return created;
            if (!node.TryGetProperty("v", out var values))
                continue;
            foreach (var value in values.EnumerateArray())
            {
                var name = value.GetProperty("n").GetString() ?? "";
                var kind = Enum.TryParse<RegistryValueKind>(value.GetProperty("t").GetString(), out var parsed) ? parsed : RegistryValueKind.String;
                var payload = value.GetProperty("d").GetString() ?? "";
                object data = kind switch
                {
                    RegistryValueKind.DWord => int.TryParse(payload, out var d) ? d : 0,
                    RegistryValueKind.QWord => long.TryParse(payload, out var q) ? q : 0L,
                    RegistryValueKind.MultiString => payload.Split('\0'),
                    RegistryValueKind.Binary or RegistryValueKind.None => Convert.FromBase64String(payload),
                    _ => payload
                };
                var set = SetValue(hive, path, name, data, kind, view, confirm: true);
                if (set.Status != RegistryWriteStatus.Ok)
                    return set;
            }
        }
        return new RegistryWriteResult(RegistryWriteStatus.Ok, hive, "", null, null);
    }

    private bool Walk(RegistryHiveKind hive, string path, RegistryViewKind view, List<Dictionary<string, object?>> nodes)
    {
        if (nodes.Count >= MaxSnapshotKeys)
            return false;
        var snap = GetKey(hive, path, view, RegistryDetailLevel.Full);
        if (snap is null)
            return true;
        var values = snap.Values.Select(v => new Dictionary<string, object?>
        {
            ["n"] = v.Name,
            ["t"] = v.Type.ToString(),
            ["d"] = PackValue(v)
        }).ToList();
        nodes.Add(new Dictionary<string, object?> { ["p"] = path, ["v"] = values });
        return snap.SubKeyNames.All(child => Walk(hive, path + "\\" + child, view, nodes));
    }

    private static string PackValue(RegistryValueInfo value) => value.Type switch
    {
        RegistryValueKind.Binary or RegistryValueKind.None when value.Data is byte[] bytes => Convert.ToBase64String(bytes),
        RegistryValueKind.MultiString when value.Data is string[] parts => string.Join("\u0000", parts),
        _ => value.DataText ?? value.Data?.ToString() ?? ""
    };
}
