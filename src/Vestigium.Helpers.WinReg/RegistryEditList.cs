using System.Text.Json;
using Vestigium.Helpers;

namespace Vestigium.Helpers.WinReg;

public sealed class RegistryEditOp
{
    public required string Op { get; init; }
    public required RegistryHiveKind Hive { get; init; }
    public required string Path { get; init; }
    public string? Name { get; init; }
    public string? From { get; init; }
    public string? To { get; init; }
    public RegistryValueKind Type { get; init; } = RegistryValueKind.String;
    public JsonElement? Data { get; init; }
    public bool Recursive { get; init; }
    public RegistryViewKind View { get; init; }
}

public sealed class RegistryEditList
{
    public const string Schema = "vest-regedits/1";

    public string? Label { get; init; }
    public List<RegistryEditOp> Ops { get; } = [];

    public static RegistryEditList Load(string path)
    {
        path = HelperGuard.FileExists(path, nameof(path));
        using var doc = JsonDocument.Parse(File.ReadAllText(path));
        var root = doc.RootElement;
        var list = new RegistryEditList { Label = root.TryGetProperty("label", out var label) ? label.GetString() : null };
        if (root.TryGetProperty("ops", out var ops))
        {
            foreach (var op in ops.EnumerateArray())
                list.Ops.Add(ReadOp(op));
        }
        return list;
    }

    public void Save(string path)
    {
        path = HelperGuard.NotBlank(path, nameof(path));
        var rows = new List<Dictionary<string, object?>>();
        foreach (var op in Ops)
        {
            var row = new Dictionary<string, object?>
            {
                ["op"] = op.Op,
                ["hive"] = op.Hive.ToString(),
                ["path"] = op.Path,
                ["view"] = op.View.ToString()
            };
            if (op.Name is not null) row["name"] = op.Name;
            if (op.From is not null) row["from"] = op.From;
            if (op.To is not null) row["to"] = op.To;
            if (op.Op is "SetValue") row["type"] = op.Type.ToString();
            if (op.Data is { } data) row["data"] = JsonSerializer.Deserialize<object>(data.GetRawText());
            if (op.Recursive) row["recursive"] = true;
            rows.Add(row);
        }

        var payload = new Dictionary<string, object?>
        {
            ["schema"] = Schema,
            ["label"] = Label,
            ["ops"] = rows
        };
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) is { Length: > 0 } dir ? dir : ".");
        File.WriteAllText(path, JsonSerializer.Serialize(payload, new JsonSerializerOptions { WriteIndented = true }));
    }

    public RegistryEditList CreateKey(RegistryHiveKind hive, string path, RegistryViewKind view = RegistryViewKind.Default)
    {
        Ops.Add(new RegistryEditOp { Op = "CreateKey", Hive = hive, Path = path, View = view });
        return this;
    }

    public RegistryEditList DeleteKey(RegistryHiveKind hive, string path, bool recursive = false, RegistryViewKind view = RegistryViewKind.Default)
    {
        Ops.Add(new RegistryEditOp { Op = "DeleteKey", Hive = hive, Path = path, Recursive = recursive, View = view });
        return this;
    }

    public RegistryEditList SetValue(RegistryHiveKind hive, string path, string name, object? data, RegistryValueKind type = RegistryValueKind.String, RegistryViewKind view = RegistryViewKind.Default)
    {
        Ops.Add(new RegistryEditOp
        {
            Op = "SetValue",
            Hive = hive,
            Path = path,
            Name = name,
            Type = type,
            Data = JsonSerializer.SerializeToElement(data),
            View = view
        });
        return this;
    }

    public RegistryEditList DeleteValue(RegistryHiveKind hive, string path, string name, RegistryViewKind view = RegistryViewKind.Default)
    {
        Ops.Add(new RegistryEditOp { Op = "DeleteValue", Hive = hive, Path = path, Name = name, View = view });
        return this;
    }

    public RegistryEditList RenameValue(RegistryHiveKind hive, string path, string from, string to, RegistryViewKind view = RegistryViewKind.Default)
    {
        Ops.Add(new RegistryEditOp { Op = "RenameValue", Hive = hive, Path = path, From = from, To = to, View = view });
        return this;
    }

    public RegistryEditList RenameKey(RegistryHiveKind hive, string from, string to, RegistryViewKind view = RegistryViewKind.Default)
    {
        Ops.Add(new RegistryEditOp { Op = "RenameKey", Hive = hive, Path = from, To = to, View = view });
        return this;
    }

    private static RegistryEditOp ReadOp(JsonElement op)
    {
        var hive = Enum.Parse<RegistryHiveKind>(op.GetProperty("hive").GetString() ?? "CurrentUser");
        var path = op.GetProperty("path").GetString() ?? "";
        var view = op.TryGetProperty("view", out var viewEl) && Enum.TryParse<RegistryViewKind>(viewEl.GetString(), out var parsedView)
            ? parsedView
            : RegistryViewKind.Default;
        var type = op.TryGetProperty("type", out var typeEl) && Enum.TryParse<RegistryValueKind>(typeEl.GetString(), out var parsedType)
            ? parsedType
            : RegistryValueKind.String;
        return new RegistryEditOp
        {
            Op = op.GetProperty("op").GetString() ?? "",
            Hive = hive,
            Path = path,
            Name = op.TryGetProperty("name", out var name) ? name.GetString() : null,
            From = op.TryGetProperty("from", out var from) ? from.GetString() : null,
            To = op.TryGetProperty("to", out var to) ? to.GetString() : null,
            Type = type,
            Data = op.TryGetProperty("data", out var data) ? data.Clone() : null,
            Recursive = op.TryGetProperty("recursive", out var rec) && rec.GetBoolean(),
            View = view
        };
    }
}
