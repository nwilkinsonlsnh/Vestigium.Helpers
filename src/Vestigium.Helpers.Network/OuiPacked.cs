using System.Reflection;

namespace Vestigium.Helpers.Network;

internal static class OuiPacked
{
    public const string ResourceName = "Vestigium.Helpers.Network.Data.oui-snapshot.txt";

    private static readonly Lazy<IReadOnlyDictionary<string, string>> Cache = new(Load);

    public static IReadOnlyDictionary<string, string> Registry() => Cache.Value;

    public static OuiLookupResult Lookup(string macOrOui)
        => OuiRegistry.Lookup(macOrOui, Registry());

    private static IReadOnlyDictionary<string, string> Load()
    {
        var assembly = typeof(OuiPacked).Assembly;
        using var stream = assembly.GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException("Packed OUI snapshot resource is missing.");
        using var reader = new StreamReader(stream);
        var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        while (reader.ReadLine() is { } raw)
        {
            var line = raw.Trim();
            if (line.Length == 0 || line[0] is '#' or ';')
                continue;
            var parts = line.Split(new[] { ',', '\t', '|', ';' }, 2, StringSplitOptions.TrimEntries);
            if (parts.Length < 2)
                continue;
            map[OuiRegistry.Normalize(parts[0])] = parts[1];
        }

        NetworkLog.Success("Address", $"oui packed rows={map.Count}");
        return map;
    }
}
