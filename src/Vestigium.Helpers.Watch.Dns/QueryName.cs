namespace Vestigium.Helpers.Watch.Dns;

public static class QueryName
{
    public static bool TryNormalize(string? value, out string name, out string? reject)
    {
        name = "";
        var trimmed = value?.Trim() ?? "";
        if (trimmed.Length == 0)
        {
            reject = "Name is required.";
            return false;
        }

        if (trimmed.Contains("://", StringComparison.Ordinal))
        {
            reject = "A scheme is not a query name.";
            return false;
        }

        if (trimmed.Contains('/'))
        {
            reject = "A path is not a query name.";
            return false;
        }

        if (trimmed.EndsWith('.'))
            trimmed = trimmed[..^1];

        if (trimmed.Length == 0 || trimmed.Split('.').Any(label => label.Length == 0))
        {
            reject = "A name cannot have an empty label.";
            return false;
        }

        name = trimmed.ToLowerInvariant();
        reject = null;
        return true;
    }
}
