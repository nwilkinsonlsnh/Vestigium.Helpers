using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class OuiRegistry
{
    public static IReadOnlyDictionary<string, string> Load(string path)
    {
        var file = HelperGuard.NotBlank(path, nameof(path));
        if (!File.Exists(file))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, "Address", nameof(Load), "missing file");
            throw new FileNotFoundException("OUI registry file was not found.", file);
        }

        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var raw in File.ReadLines(file))
        {
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

            var parts = line.Split([',', '	', '|', ';'], 2, StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
                continue;
            map[Normalize(parts[0])] = parts[1];
        }

        NetworkLog.Success("Address", $"oui file rows={map.Count}");
        return map;
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
        hex = hex[..6].ToUpperInvariant();
        return $"{hex[0]}{hex[1]}:{hex[2]}{hex[3]}:{hex[4]}{hex[5]}";
    }

    static string? ExtractJson(string line, string name)
    {
        var key = $""{name}"";
        var i = line.IndexOf(key, StringComparison.OrdinalIgnoreCase);
        if (i < 0) return null;
        var colon = line.IndexOf(':', i + key.Length);
        if (colon < 0) return null;
        var q1 = line.IndexOf('"', colon + 1);
        if (q1 < 0) return null;
        var q2 = line.IndexOf('"', q1 + 1);
        if (q2 < 0) return null;
        return line[(q1 + 1)..q2];
    }
}
