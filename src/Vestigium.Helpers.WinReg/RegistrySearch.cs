using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.WinReg;

public enum RegistrySearchMode
{
    StartsWith = 0,
    EndsWith = 1,
    Contains = 2
}

[Flags]
public enum RegistrySearchFields
{
    KeyName = 1,
    ValueName = 2,
    ValueData = 4
}

public sealed class RegistryHit
{
    public required RegistryHiveKind Hive { get; init; }
    public required string Path { get; init; }
    public string? ValueName { get; init; }
    public RegistrySearchFields MatchedOn { get; init; }
    public required RegistryKeyInfo Key { get; init; }
}

public sealed partial class RegistryClient
{
    public const int MaxSearchResults = 256;
    public const int MaxSearchDepth = 32;

    public IReadOnlyList<RegistryHit> Search(
        RegistryHiveKind hive,
        string? key,
        string term,
        RegistrySearchMode mode = RegistrySearchMode.Contains,
        RegistrySearchFields fields = RegistrySearchFields.KeyName | RegistrySearchFields.ValueName,
        int maxDepth = 16,
        int maxResults = MaxSearchResults,
        RegistryViewKind view = RegistryViewKind.Default)
    {
        var needle = HelperGuard.NotBlank(term, nameof(term));
        HelperGuard.Require(maxDepth >= 0 && maxDepth <= MaxSearchDepth, nameof(maxDepth), $"MaxDepth cap is {MaxSearchDepth}.");
        HelperGuard.Require(maxResults > 0 && maxResults <= MaxSearchResults, nameof(maxResults), $"MaxResults cap is {MaxSearchResults}.");
        if (fields == 0)
            fields = RegistrySearchFields.KeyName | RegistrySearchFields.ValueName;

        var start = RegistryPath.Normalize(key);
        var hits = new List<RegistryHit>();
        Walk(hive, start, view, needle, mode, fields, maxDepth, maxResults, 0, hits);
        HelperLog.Information(HelperLog.AppIds.WinReg, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"Search hive={hive} path={start} n={hits.Count}");
        return hits;
    }

    private void Walk(
        RegistryHiveKind hive,
        string path,
        RegistryViewKind view,
        string needle,
        RegistrySearchMode mode,
        RegistrySearchFields fields,
        int maxDepth,
        int maxResults,
        int depth,
        List<RegistryHit> hits)
    {
        if (hits.Count >= maxResults || depth > maxDepth)
            return;

        var snap = GetKey(hive, path, view, fields.HasFlag(RegistrySearchFields.ValueData) || fields.HasFlag(RegistrySearchFields.ValueName)
            ? RegistryDetailLevel.Full
            : RegistryDetailLevel.Slim);
        if (snap is null)
            return;

        if (fields.HasFlag(RegistrySearchFields.KeyName) && Match(snap.Name, needle, mode))
        {
            hits.Add(new RegistryHit { Hive = hive, Path = path, MatchedOn = RegistrySearchFields.KeyName, Key = snap });
            if (hits.Count >= maxResults)
                return;
        }

        if (fields.HasFlag(RegistrySearchFields.ValueName) || fields.HasFlag(RegistrySearchFields.ValueData))
        {
            foreach (var value in snap.Values)
            {
                if (hits.Count >= maxResults)
                    return;
                if (fields.HasFlag(RegistrySearchFields.ValueName) && Match(value.Name, needle, mode))
                {
                    hits.Add(new RegistryHit { Hive = hive, Path = path, ValueName = value.Name, MatchedOn = RegistrySearchFields.ValueName, Key = snap });
                    continue;
                }

                if (fields.HasFlag(RegistrySearchFields.ValueData) && IsText(value.Type) && MatchText(value, needle, mode))
                    hits.Add(new RegistryHit { Hive = hive, Path = path, ValueName = value.Name, MatchedOn = RegistrySearchFields.ValueData, Key = snap });
            }
        }

        foreach (var child in snap.SubKeyNames)
        {
            if (hits.Count >= maxResults)
                return;
            var next = string.IsNullOrEmpty(path) ? child : path + "\\" + child;
            Walk(hive, next, view, needle, mode, fields, maxDepth, maxResults, depth + 1, hits);
        }
    }

    private static bool IsText(RegistryValueKind kind)
        => kind is RegistryValueKind.String or RegistryValueKind.ExpandString or RegistryValueKind.MultiString;

    private static bool MatchText(RegistryValueInfo value, string needle, RegistrySearchMode mode) => value.Data switch
    {
        string text => Match(text, needle, mode),
        string[] parts => parts.Any(p => Match(p, needle, mode)),
        _ => Match(value.DataText, needle, mode)
    };

    private static bool Match(string? haystack, string needle, RegistrySearchMode mode)
    {
        if (string.IsNullOrEmpty(haystack))
            return false;
        return mode switch
        {
            RegistrySearchMode.StartsWith => haystack.StartsWith(needle, StringComparison.OrdinalIgnoreCase),
            RegistrySearchMode.EndsWith => haystack.EndsWith(needle, StringComparison.OrdinalIgnoreCase),
            _ => haystack.Contains(needle, StringComparison.OrdinalIgnoreCase)
        };
    }
}
