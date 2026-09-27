namespace Vestigium.Helpers.PerfMon;

/// <summary>
/// One PDH path. Instance may be empty (Memory object). Unit may be empty until a probe fills it.
/// </summary>
public sealed record CounterPath
{
    public CounterPath(string category, string counter, string instance = "", string unit = "")
    {
        Category = RequireName(category, nameof(category));
        Counter = RequireName(counter, nameof(counter));
        Instance = instance?.Trim() ?? string.Empty;
        Unit = unit?.Trim() ?? string.Empty;
    }

    public string Category { get; }
    public string Counter { get; }
    public string Instance { get; }
    public string Unit { get; }

    internal string Key => $"{Category}\u001f{Counter}\u001f{Instance}";

    private static string RequireName(string? value, string paramName)
    {
        var trimmed = value?.Trim() ?? string.Empty;
        if (trimmed.Length == 0)
            throw new ArgumentException("Category and counter are required.", paramName);
        return trimmed;
    }
}
