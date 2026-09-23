using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class OuiRegistry
{
    internal const long MaxFileBytes = 8L * 1024 * 1024;
    internal const int MaxRows = 200_000;

    public static IReadOnlyDictionary<string, string> Load(string path)
    {
        var file = HelperGuard.NotBlank(path, nameof(path));
        if (!File.Exists(file))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, "Address", nameof(Load), "missing file");
            throw new FileNotFoundException("OUI registry file was not found.", file);
        }

        GuardFile(new FileInfo(file));

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var rows = 0;
        foreach (var raw in File.ReadLines(file))
        {
            rows++;
            GuardRow(rows);
            var line = raw.Trim();
            if (line.Length == 0 || line[0] is '#' or ';')
                continue;
            if (line.StartsWith('{') && line.Contains("oui", StringComparison.OrdinalIgnoreCase))
            {
                var oui = ExtractJson(line, "oui");
                var vendor = ExtractJson(line, "vendor");
                if (oui is not null && vendor is not null)
                    map[Normalize(oui)] = vendor;
                continue;
            }

            var parts = line.Split([',', '\t', '|', ';'], 2, StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
                continue;
            map[Normalize(parts[0])] = parts[1];
        }

        NetworkLog.Success("Address", $"oui file rows={map.Count}");
        return map;
    }

    internal static void GuardFile(FileInfo info)
    {
        if (info.Length <= MaxFileBytes)
            return;

        HelperLog.Reject(HelperLog.AppIds.Network, "Address", nameof(Load), "oui file too large");
        throw new ArgumentException($"OUI registry file exceeds {MaxFileBytes} bytes.", nameof(info));
    }

    internal static void GuardRow(int rows)
    {
        if (rows <= MaxRows)
            return;

        HelperLog.Reject(HelperLog.AppIds.Network, "Address", nameof(Load), "oui file too many rows");
        throw new ArgumentException($"OUI registry file exceeds {MaxRows} rows.", nameof(rows));
    }

    public static OuiLookupResult Lookup(string macOrOui, IReadOnlyDictionary<string, string> map)
    {
        var mac = MacEngine.Parse(macOrOui);
        var key = Normalize(mac.Oui24);
        map.TryGetValue(key, out var vendor);
        return new OuiLookupResult(
            mac.Colon,
            vendor,
            vendor is null ? OuiSource.None : OuiSource.File,
            OuiLookupOptions.Disclaimer + " Source=file snapshot; not a live IEEE pull.");
    }

    public static string Normalize(string oui)
    {
        var hex = new string(oui.Where(Uri.IsHexDigit).ToArray());
        if (hex.Length < 6)
            return hex.ToUpperInvariant();
        hex = hex.Substring(0, 6).ToUpperInvariant();
        return hex.Substring(0, 2) + ":" + hex.Substring(2, 2) + ":" + hex.Substring(4, 2);
    }

    private static string? ExtractJson(string line, string name)
    {
        var key = "\"" + name + "\"";
        var i = line.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (i < 0)
            return null;
        var colon = line.IndexOf(':', i + key.Length);
        if (colon < 0)
            return null;
        var q1 = line.IndexOf('"', colon + 1);
        if (q1 < 0)
            return null;
        var q2 = line.IndexOf('"', q1 + 1);
        if (q2 < 0)
            return null;
        return line.Substring(q1 + 1, q2 - q1 - 1);
    }
}
