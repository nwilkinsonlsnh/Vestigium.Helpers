using System.Diagnostics.CodeAnalysis;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Processes;

/// <summary>Process table helpers. Phase 3: tree and threads.</summary>
public static class ProcessHelper
{
    public const int DefaultMaxSearchResults = 256;
    public const int MaxSearchResultsCap = 4096;

    public static string Identity => "Vestigium.Helpers.Processes";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Processes;
        HelperLog.Information(app, VestigiumStatus.Pending, app, "Describing current process identity.");
        HelperLog.Information(app, VestigiumStatus.Success, app, "Process probe complete. Identity=" + Identity);
        return Identity;
    }

    public static IReadOnlyList<ProcessInfo> List(ProcessDetailLevel level = ProcessDetailLevel.Slim)
    {
        var app = HelperLog.AppIds.Processes;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Inventory, "List", "level=" + level);
        var rows = ProcessSnapshotter.Capture(level);
        HelperLog.Information(app, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"List n={rows.Count} level={level}");
        return rows;
    }

    public static ProcessInfo? Get(int pid, ProcessDetailLevel level = ProcessDetailLevel.Full)
    {
        HelperGuard.InRange(pid, 1, nameof(pid));
        var app = HelperLog.AppIds.Processes;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Inventory, "Get", "pid=" + pid);
        var row = ProcessSnapshotter.CapturePid(pid, level);
        HelperLog.Information(app, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, row is null ? $"Get pid={pid} gone" : $"Get pid={pid} name={row.Name}");
        return row;
    }

    public static bool TryGet(int pid, [NotNullWhen(true)] out ProcessInfo? info)
    {
        if (pid <= 0)
        {
            info = null;
            return false;
        }

        info = ProcessSnapshotter.CapturePid(pid, ProcessDetailLevel.Full);
        return info is not null;
    }

    public static IReadOnlyList<ProcessInfo> Search(
        string term,
        ProcessSearchMode mode,
        ProcessSearchFields fields = ProcessSearchFields.Default,
        ProcessDetailLevel level = ProcessDetailLevel.Slim,
        int maxResults = DefaultMaxSearchResults)
    {
        var needle = HelperGuard.NotBlank(term, nameof(term)).Trim();
        HelperGuard.InRange(maxResults, 1, nameof(maxResults));
        HelperGuard.Require(maxResults <= MaxSearchResultsCap, nameof(maxResults), "maxResults must be at most 4096.");
        if (fields == 0)
            fields = ProcessSearchFields.Default;

        var app = HelperLog.AppIds.Processes;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Inventory, "Search", $"mode={mode} max={maxResults}");
        var hits = new List<ProcessInfo>();
        foreach (var row in ProcessSnapshotter.Capture(level))
        {
            if (!Matches(row, needle, mode, fields))
                continue;
            hits.Add(row);
            if (hits.Count >= maxResults)
                break;
        }

        HelperLog.Information(app, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"Search mode={mode} hits={hits.Count} max={maxResults}");
        return hits;
    }

    public static ProcessTree GetTree(int pid, ProcessDetailLevel level = ProcessDetailLevel.Identity)
    {
        HelperGuard.InRange(pid, 1, nameof(pid));
        var app = HelperLog.AppIds.Processes;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Inventory, "GetTree", "pid=" + pid);
        var tree = ProcessTreeWalker.Build(pid, level)
            ?? throw new InvalidOperationException($"Process {pid} is gone.");
        HelperLog.Information(app, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"GetTree pid={pid} nodes={tree.Flatten().Count}");
        return tree;
    }

    public static IReadOnlyList<ProcessInfo> GetChildren(int pid)
    {
        HelperGuard.InRange(pid, 1, nameof(pid));
        return ProcessTreeWalker.ChildrenOf(pid);
    }

    public static IReadOnlyList<ProcessInfo> GetDescendants(int pid)
    {
        HelperGuard.InRange(pid, 1, nameof(pid));
        return ProcessTreeWalker.DescendantsOf(pid);
    }

    public static IReadOnlyList<ThreadInfo> GetThreads(int pid, bool includeStack = false)
    {
        HelperGuard.InRange(pid, 1, nameof(pid));
        var app = HelperLog.AppIds.Processes;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Inventory, "GetThreads", "pid=" + pid);
        var rows = ProcessThreadReader.Capture(pid, includeStack);
        HelperLog.Information(app, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"GetThreads pid={pid} n={rows.Count}");
        return rows;
    }

    public static void SetComment(int pid, string? comment, bool persist = false)
    {
        HelperGuard.InRange(pid, 1, nameof(pid));
        var row = Get(pid, ProcessDetailLevel.Slim)
            ?? throw new InvalidOperationException($"Process {pid} is gone.");
        ProcessCommentStore.Set(ProcessCommentStore.Key(row.ImagePath, row.Name), comment, persist);
    }

    private static bool Matches(ProcessInfo row, string term, ProcessSearchMode mode, ProcessSearchFields fields)
    {
        if (fields.HasFlag(ProcessSearchFields.Name) && Compare(row.Name, term, mode))
            return true;
        if (fields.HasFlag(ProcessSearchFields.ImagePath) && Compare(row.ImagePath, term, mode))
            return true;
        if (fields.HasFlag(ProcessSearchFields.CommandLine) && Compare(row.CommandLine, term, mode))
            return true;
        if (fields.HasFlag(ProcessSearchFields.WindowTitle) && Compare(row.WindowTitle, term, mode))
            return true;
        return false;
    }

    private static bool Compare(string? value, string term, ProcessSearchMode mode)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return mode switch
        {
            ProcessSearchMode.StartsWith => value.StartsWith(term, StringComparison.OrdinalIgnoreCase),
            ProcessSearchMode.EndsWith => value.EndsWith(term, StringComparison.OrdinalIgnoreCase),
            ProcessSearchMode.Contains => value.Contains(term, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
