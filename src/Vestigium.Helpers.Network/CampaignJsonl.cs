using System.Text.Json.Nodes;
using Vestigium.Helpers.Json;

namespace Vestigium.Helpers.Network;

internal static class CampaignJsonl
{
    static readonly JsonWriteOptions Compact = new() { WriteIndented = false };

    public static void Append(string path, object record)
    {
        var dir = Path.GetDirectoryName(path);
        if (!string.IsNullOrWhiteSpace(dir))
            Directory.CreateDirectory(dir);
        var line = JsonHelper.ToJson(record, Compact);
        if (line.IndexOfAny(['\r', '\n']) >= 0)
            line = string.Join(' ', line.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries));
        File.AppendAllText(path, line + "\n");
    }

    public static IReadOnlyList<Dictionary<string, object?>> Read(string path)
    {
        if (!File.Exists(path))
            return [];
        using var session = JsonHelper.OpenJsonl(path);
        var rows = new List<Dictionary<string, object?>>(session.RecordCount);
        for (var i = 0; i < session.RecordCount; i++)
        {
            var node = session.Record(i);
            if (node is not JsonObject obj)
                continue;
            var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            foreach (var prop in obj)
                row[prop.Key] = prop.Value is null ? null : prop.Value.ToJsonString();
            rows.Add(row);
        }

        return rows;
    }

    public static bool HasTerminalWindow(IReadOnlyList<Dictionary<string, object?>> rows, string campaignId, string date, string localTime)
    {
        foreach (var row in rows)
        {
            if (!KindIs(row, "windowSummary") && !KindIs(row, "windowMissed"))
                continue;
            if (!string.Equals(Get(row, "campaignId")?.Trim('"'), campaignId, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.Equals(Get(row, "date")?.Trim('"'), date, StringComparison.OrdinalIgnoreCase))
                continue;
            if (!string.Equals(Get(row, "localTime")?.Trim('"'), localTime, StringComparison.OrdinalIgnoreCase))
                continue;
            return true;
        }

        return false;
    }

    public static bool HasKind(IReadOnlyList<Dictionary<string, object?>> rows, string campaignId, string kind)
        => rows.Any(r => KindIs(r, kind) && string.Equals(Get(r, "campaignId")?.Trim('"'), campaignId, StringComparison.OrdinalIgnoreCase));

    static bool KindIs(Dictionary<string, object?> row, string kind)
        => string.Equals(Get(row, "kind")?.Trim('"'), kind, StringComparison.OrdinalIgnoreCase);

    static string? Get(Dictionary<string, object?> row, string key)
        => row.TryGetValue(key, out var value) ? value?.ToString() : null;
}
