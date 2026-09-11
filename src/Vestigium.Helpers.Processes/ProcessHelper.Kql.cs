using Vestigium.Helpers;
using Vestigium.Helpers.Kql;
using Vestigium.Logging;

namespace Vestigium.Helpers.Processes;

public static partial class ProcessHelper
{
    public static IReadOnlyList<ProcessInfo> Search(
        string query,
        ProcessDetailLevel level = ProcessDetailLevel.Slim,
        int maxResults = DefaultMaxSearchResults)
    {
        HelperGuard.NotBlank(query, nameof(query));
        HelperGuard.InRange(maxResults, 1, nameof(maxResults));
        HelperGuard.Require(maxResults <= MaxSearchResultsCap, nameof(maxResults), "maxResults must be at most 4096.");

        using var session = KqlHelper.Create(KqlPack.Process);
        var compiled = KqlHelper.Compile(query, session);
        if (!compiled.Ok)
        {
            HelperLog.Reject(compiled.Error?.Message ?? "query compile failed");
            throw new ArgumentException(compiled.Error?.Message ?? "query compile failed", nameof(query));
        }

        var capture = ProcessKqlLevel.Resolve(level, compiled.Query!.Expression, session);
        var app = HelperLog.AppIds.Processes;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Query, "Search", $"kql max={maxResults} level={capture}");
        var hits = new List<ProcessInfo>();
        foreach (var row in ProcessSnapshotter.Capture(capture))
        {
            if (compiled.Query.Matches(new ProcessKqlRow(row)))
                hits.Add(row);
        }

        var taken = ProcessSearchSort.TakeStable(hits, maxResults);
        HelperLog.Information(app, VestigiumStatus.Success, HelperLog.Subcategories.Query, $"Search kql hits={taken.Count} max={maxResults} level={capture}");
        return taken;
    }

    public static IReadOnlyList<ThreadInfo> SearchThreads(int pid, string query)
    {
        HelperGuard.InRange(pid, 1, nameof(pid));
        HelperGuard.NotBlank(query, nameof(query));
        using var session = KqlHelper.Create(KqlPack.Thread);
        var compiled = KqlHelper.Compile(query, session);
        if (!compiled.Ok)
        {
            HelperLog.Reject(compiled.Error?.Message ?? "query compile failed");
            throw new ArgumentException(compiled.Error?.Message ?? "query compile failed", nameof(query));
        }

        var app = HelperLog.AppIds.Processes;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Query, "SearchThreads", "pid=" + pid);
        var hits = ProcessThreadReader.Capture(pid, includeStack: false)
            .Where(row => compiled.Query!.Matches(new ThreadKqlRow(row)))
            .ToArray();
        HelperLog.Information(app, VestigiumStatus.Success, HelperLog.Subcategories.Query, $"SearchThreads pid={pid} hits={hits.Length}");
        return hits;
    }

    public static bool MatchSystem(string query)
    {
        HelperGuard.NotBlank(query, nameof(query));
        using var session = KqlHelper.Create(KqlPack.System);
        var compiled = KqlHelper.Compile(query, session);
        if (!compiled.Ok)
        {
            HelperLog.Reject(compiled.Error?.Message ?? "query compile failed");
            throw new ArgumentException(compiled.Error?.Message ?? "query compile failed", nameof(query));
        }

        var counters = GetSystemCounters();
        return compiled.Query!.Matches(new SystemKqlRow(counters));
    }

    public static IProcessQueryWatcher Watch(string query, TimeSpan interval, ProcessWatchFields fields)
    {
        HelperGuard.NotBlank(query, nameof(query));
        return new ProcessQueryWatcher(query, RequireInterval(interval), fields);
    }
}
