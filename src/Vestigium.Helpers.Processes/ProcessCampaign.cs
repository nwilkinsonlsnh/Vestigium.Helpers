using System.Text.Json;
using Vestigium.Helpers;
using Vestigium.Helpers.Json;
using Vestigium.Logging;

namespace Vestigium.Helpers.Processes;

public sealed class ProcessCampaign : IDisposable
{
    public const int DefaultMaxMatches = 64;
    public const int MaxMatchesCap = 256;

    private readonly CancellationTokenSource _cts = new();
    private readonly object _file = new();
    private int _disposed;
    private HashSet<string> _open = new(StringComparer.OrdinalIgnoreCase);
    private bool _loggedTruncated;

    internal ProcessCampaign(ProcessCampaignRecipe recipe, string folder)
    {
        Recipe = recipe;
        CampaignId = recipe.Name;
        Folder = folder;
        SamplePath = Path.Combine(folder, "samples.jsonl");
        RecipePath = Path.Combine(folder, "recipe.json");
        State = ProcessCampaignState.Idle;
    }

    public string CampaignId { get; }
    public ProcessCampaignRecipe Recipe { get; }
    public ProcessCampaignState State { get; private set; }
    public string SamplePath { get; }
    public string RecipePath { get; }
    internal string Folder { get; }
    public event EventHandler<ProcessCampaignTick>? Sampled;
    public event EventHandler<ProcessCampaignWindowEvent>? WindowChanged;

    public async Task RunAsync(CancellationToken cancellation = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _cts.Token);
        var token = linked.Token;
        State = ProcessCampaignState.Waiting;
        Log("Campaign start name=" + CampaignId);
        try
        {
            while (!token.IsCancellationRequested)
            {
                TickOnce();
                try { await Task.Delay(Recipe.SampleInterval, token).ConfigureAwait(false); }
                catch (OperationCanceledException) { break; }
            }
        }
        finally
        {
            State = ProcessCampaignState.Stopped;
            Log("Campaign stop name=" + CampaignId);
        }
    }

    public void Stop() => _cts.Cancel();

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0)
            return;
        Stop();
        _cts.Dispose();
    }

    internal void TickOnce()
    {
        var now = ProcessTestHooks.Clock();
        var zone = ResolveZone();
        var open = Recipe.Windows.Where(window => IsOpen(window, now, zone)).Select(window => window.Name).ToArray();
        RaiseWindowChanges(open, now);

        if (open.Length == 0)
        {
            State = ProcessCampaignState.Waiting;
            _loggedTruncated = false;
            return;
        }

        State = ProcessCampaignState.Sampling;
        var hits = ProcessHelper.Search(
            Recipe.Match.Term,
            Recipe.Match.Mode,
            Recipe.Match.Fields,
            ProcessDetailLevel.Slim,
            Recipe.MaxMatches + 1);
        var truncated = hits.Count > Recipe.MaxMatches;
        if (truncated)
        {
            hits = hits.Take(Recipe.MaxMatches).ToArray();
            if (!_loggedTruncated)
            {
                HelperLog.Warning(
                    HelperLog.AppIds.Processes,
                    VestigiumStatus.Warning,
                    HelperLog.Subcategories.Campaign,
                    "Campaign Truncated name=" + CampaignId + " max=" + Recipe.MaxMatches);
                _loggedTruncated = true;
            }
        }

        SystemCounters? system = null;
        if (Recipe.IncludeSystemCounters)
            system = ProcessHelper.GetSystemCounters();

        WriteLines(now, open, hits, system);
        Sampled?.Invoke(this, new ProcessCampaignTick
        {
            Timestamp = now,
            OpenWindows = open,
            Processes = hits,
            System = system,
            Truncated = truncated
        });
    }

    private void WriteLines(DateTimeOffset now, IReadOnlyList<string> open, IReadOnlyList<ProcessInfo> hits, SystemCounters? system)
    {
        var stamp = now.ToString("o");
        var windows = string.Join(",", open.Select(name => JsonSerializer.Serialize(name)));
        var lines = new List<string>(hits.Count + 1);
        foreach (var row in hits)
        {
            lines.Add(
                "{" +
                "\"kind\":\"process\"," +
                "\"campaign\":" + JsonSerializer.Serialize(CampaignId) + "," +
                "\"windows\":[" + windows + "]," +
                "\"ts\":" + JsonSerializer.Serialize(stamp) + "," +
                "\"pid\":" + row.Pid + "," +
                "\"name\":" + JsonSerializer.Serialize(row.Name) + "," +
                "\"imagePath\":" + JsonSerializer.Serialize(row.ImagePath) + "," +
                "\"cpuTime\":" + JsonSerializer.Serialize(row.CpuTime?.ToString()) + "," +
                "\"privateBytes\":" + (row.PrivateBytes?.ToString() ?? "null") + "," +
                "\"workingSet\":" + (row.WorkingSet?.ToString() ?? "null") +
                "}");
        }

        if (system is not null)
        {
            lines.Add(
                "{" +
                "\"kind\":\"system\"," +
                "\"campaign\":" + JsonSerializer.Serialize(CampaignId) + "," +
                "\"windows\":[" + windows + "]," +
                "\"ts\":" + JsonSerializer.Serialize(stamp) + "," +
                "\"cpuPercent\":" + (system.CpuPercent?.ToString(System.Globalization.CultureInfo.InvariantCulture) ?? "null") + "," +
                "\"commitCurrent\":" + (system.CommitCurrent?.ToString() ?? "null") + "," +
                "\"physicalAvailable\":" + (system.PhysicalAvailable?.ToString() ?? "null") +
                "}");
        }

        lock (_file)
        {
            Directory.CreateDirectory(Folder);
            File.AppendAllLines(SamplePath, lines);
        }
    }

    private void RaiseWindowChanges(IReadOnlyList<string> open, DateTimeOffset now)
    {
        var next = new HashSet<string>(open, StringComparer.OrdinalIgnoreCase);
        foreach (var name in next.Except(_open, StringComparer.OrdinalIgnoreCase))
        {
            Log("Campaign window-open name=" + CampaignId + " window=" + name);
            WindowChanged?.Invoke(this, new ProcessCampaignWindowEvent { WindowName = name, Open = true, Timestamp = now });
        }
        foreach (var name in _open.Except(next, StringComparer.OrdinalIgnoreCase))
        {
            Log("Campaign window-close name=" + CampaignId + " window=" + name);
            WindowChanged?.Invoke(this, new ProcessCampaignWindowEvent { WindowName = name, Open = false, Timestamp = now });
        }
        _open = next;
    }

    internal static bool IsOpen(ProcessCampaignWindow window, DateTimeOffset now, TimeZoneInfo zone)
    {
        var local = TimeZoneInfo.ConvertTime(now, zone);
        var start = local.Date + window.StartLocal.ToTimeSpan();
        var end = start + window.Duration;
        if (local >= start && local < end && DayMatches(window.Days, start.DayOfWeek))
            return true;

        var previous = start.AddDays(-1);
        var previousEnd = previous + window.Duration;
        return local >= previous && local < previousEnd && DayMatches(window.Days, previous.DayOfWeek);
    }

    private static bool DayMatches(ProcessCampaignDays days, DayOfWeek day)
    {
        if (days == 0 || days.HasFlag(ProcessCampaignDays.All))
            return true;
        var flag = day switch
        {
            DayOfWeek.Sunday => ProcessCampaignDays.Sunday,
            DayOfWeek.Monday => ProcessCampaignDays.Monday,
            DayOfWeek.Tuesday => ProcessCampaignDays.Tuesday,
            DayOfWeek.Wednesday => ProcessCampaignDays.Wednesday,
            DayOfWeek.Thursday => ProcessCampaignDays.Thursday,
            DayOfWeek.Friday => ProcessCampaignDays.Friday,
            _ => ProcessCampaignDays.Saturday
        };
        return days.HasFlag(flag);
    }

    private TimeZoneInfo ResolveZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(Recipe.TimeZoneId); }
        catch { return TimeZoneInfo.Local; }
    }

    private static void Log(string line)
        => HelperLog.Information(
            HelperLog.AppIds.Processes,
            VestigiumStatus.Success,
            HelperLog.Subcategories.Campaign,
            line);

    internal static ProcessCampaignRecipe Validate(ProcessCampaignRecipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        var name = HelperGuard.NotBlank(recipe.Name, nameof(recipe.Name)).Trim();
        HelperGuard.Require(recipe.Windows is { Count: > 0 }, nameof(recipe.Windows), "A campaign needs at least one window. Use Watch for continuous sampling.");
        ProcessHelper.RequireInterval(recipe.SampleInterval);
        HelperGuard.InRange(recipe.MaxMatches, 1, nameof(recipe.MaxMatches));
        HelperGuard.Require(recipe.MaxMatches <= MaxMatchesCap, nameof(recipe.MaxMatches), "MaxMatches cap is 256.");
        HelperGuard.NotBlank(recipe.Match.Term, nameof(recipe.Match.Term));
        foreach (var window in recipe.Windows)
        {
            HelperGuard.NotBlank(window.Name, nameof(window.Name));
            if (window.Duration < TimeSpan.FromMinutes(1) || window.Duration > TimeSpan.FromHours(24))
                throw new ArgumentOutOfRangeException(nameof(window.Duration), "Window duration must be 1 minute through 24 hours.");
        }

        return new ProcessCampaignRecipe
        {
            Name = Sanitize(name),
            Match = recipe.Match,
            Fields = recipe.Fields == 0 ? ProcessWatchFields.All : recipe.Fields,
            IncludeSystemCounters = recipe.IncludeSystemCounters,
            SampleInterval = recipe.SampleInterval,
            Windows = recipe.Windows.ToArray(),
            TimeZoneId = string.IsNullOrWhiteSpace(recipe.TimeZoneId) ? TimeZoneInfo.Local.Id : recipe.TimeZoneId,
            MaxMatches = recipe.MaxMatches
        };
    }

    internal static string Sanitize(string name)
    {
        var chars = name.Select(ch => char.IsLetterOrDigit(ch) || ch is '-' or '_' ? ch : '-').ToArray();
        var clean = new string(chars).Trim('-');
        return string.IsNullOrWhiteSpace(clean) ? "campaign" : clean;
    }

    internal static string Root()
    {
        if (!string.IsNullOrWhiteSpace(ProcessTestHooks.CampaignRoot))
            return Path.GetFullPath(ProcessTestHooks.CampaignRoot);
        return Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Vestigium", "Processes", "Campaigns");
    }

    internal static void WriteRecipe(string folder, ProcessCampaignRecipe recipe)
    {
        Directory.CreateDirectory(folder);
        JsonHelper.WriteFile(
            Path.Combine(folder, "recipe.json"),
            recipe,
            new JsonWriteOptions { Collision = JsonCollision.Overwrite });
    }

    internal static ProcessCampaignRecipe ReadRecipe(string folder)
    {
        var path = Path.Combine(folder, "recipe.json");
        return JsonHelper.FromJson<ProcessCampaignRecipe>(File.ReadAllText(path));
    }
}
