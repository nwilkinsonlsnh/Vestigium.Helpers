using System.Text.Json;
using Vestigium.Helpers;
using Vestigium.Helpers.Kql;
using Vestigium.Logging;

namespace Vestigium.Helpers.Services;

public sealed class ServiceCampaign : IDisposable
{
    public const int DefaultMaxMatches = 64;
    public const int MaxMatchesCap = 256;

    private readonly CancellationTokenSource _cts = new();
    private readonly object _file = new();
    private int _disposed;
    private HashSet<string> _open = new(StringComparer.OrdinalIgnoreCase);
    private bool _loggedTruncated;

    internal ServiceCampaign(ServiceCampaignRecipe recipe, string folder)
    {
        Recipe = recipe;
        CampaignId = recipe.Name;
        Folder = folder;
        SamplePath = Path.Combine(folder, "samples.jsonl");
        RecipePath = Path.Combine(folder, "recipe.json");
        State = ServiceCampaignState.Idle;
    }

    public string CampaignId { get; }
    public ServiceCampaignRecipe Recipe { get; }
    public ServiceCampaignState State { get; private set; }
    public string SamplePath { get; }
    public string RecipePath { get; }
    internal string Folder { get; }
    public event EventHandler<ServiceCampaignTick>? Sampled;
    public event EventHandler<ServiceCampaignWindowEvent>? WindowChanged;

    public async Task RunAsync(CancellationToken cancellation = default)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellation, _cts.Token);
        var token = linked.Token;
        State = ServiceCampaignState.Waiting;
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
            State = ServiceCampaignState.Stopped;
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
        var now = ServiceTestHooks.Clock();
        var zone = ResolveZone();
        var open = Recipe.Windows.Where(window => IsOpen(window, now, zone)).Select(window => window.Name).ToArray();
        RaiseWindowChanges(open, now);

        if (open.Length == 0)
        {
            State = ServiceCampaignState.Waiting;
            _loggedTruncated = false;
            return;
        }

        State = ServiceCampaignState.Sampling;
        IReadOnlyList<ServiceInfo> hits;
        try { hits = SearchHits(); }
        catch (Exception ex) when (ex is not ArgumentException)
        {
            HelperLog.Warning(HelperLog.AppIds.Services, VestigiumStatus.Warning, HelperLog.Subcategories.Campaign, $"Campaign tick failed name={CampaignId} type={ex.GetType().Name}");
            hits = [];
        }

        var truncated = hits.Count > Recipe.MaxMatches;
        if (truncated)
        {
            hits = hits.Take(Recipe.MaxMatches).ToArray();
            if (!_loggedTruncated)
            {
                HelperLog.Warning(HelperLog.AppIds.Services, VestigiumStatus.Warning, HelperLog.Subcategories.Campaign, "Campaign Truncated name=" + CampaignId + " max=" + Recipe.MaxMatches);
                _loggedTruncated = true;
            }
        }

        WriteLines(now, open, hits);
        Sampled?.Invoke(this, new ServiceCampaignTick
        {
            Timestamp = now,
            OpenWindows = open,
            Services = hits,
            Truncated = truncated
        });
    }

    private IReadOnlyList<ServiceInfo> SearchHits()
    {
        if (!string.IsNullOrWhiteSpace(Recipe.Query))
            return ServiceHelper.Search(Recipe.Query, ServiceDetailLevel.Slim, Recipe.Scope, Recipe.MaxMatches + 1);

        return ServiceHelper.Search(
            Recipe.Match!.Term,
            Recipe.Match.Mode,
            Recipe.Match.Fields,
            ServiceDetailLevel.Slim,
            Recipe.Scope,
            Recipe.MaxMatches + 1);
    }

    private void WriteLines(DateTimeOffset now, IReadOnlyList<string> open, IReadOnlyList<ServiceInfo> hits)
    {
        var stamp = now.ToString("o");
        var windows = string.Join(",", open.Select(name => JsonSerializer.Serialize(name)));
        var lines = new List<string>(hits.Count);
        foreach (var row in hits)
        {
            lines.Add(
                "{" +
                "\"kind\":\"service\"," +
                "\"campaign\":" + JsonSerializer.Serialize(CampaignId) + "," +
                "\"windows\":[" + windows + "]," +
                "\"ts\":" + JsonSerializer.Serialize(stamp) + "," +
                "\"name\":" + JsonSerializer.Serialize(row.Name) + "," +
                "\"displayName\":" + JsonSerializer.Serialize(row.DisplayName) + "," +
                "\"status\":" + JsonSerializer.Serialize(row.Status.ToString()) + "," +
                "\"startType\":" + JsonSerializer.Serialize(row.StartType?.ToString()) + "," +
                "\"pid\":" + (row.Pid?.ToString() ?? "null") + "," +
                "\"account\":" + JsonSerializer.Serialize(row.Account) + "," +
                "\"imagePath\":" + JsonSerializer.Serialize(row.ImagePath) + "," +
                "\"hidden\":" + (row.IsHidden ? "true" : "false") +
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
            WindowChanged?.Invoke(this, new ServiceCampaignWindowEvent { WindowName = name, Open = true, Timestamp = now });
        foreach (var name in _open.Except(next, StringComparer.OrdinalIgnoreCase))
            WindowChanged?.Invoke(this, new ServiceCampaignWindowEvent { WindowName = name, Open = false, Timestamp = now });
        _open = next;
    }

    internal static bool IsOpen(ServiceCampaignWindow window, DateTimeOffset now, TimeZoneInfo zone)
    {
        var local = TimeZoneInfo.ConvertTime(now, zone);
        var start = local.Date + window.StartLocal.ToTimeSpan();
        var end = start + window.Duration;
        if (local >= start && local < end && DayMatches(window.Days, start.DayOfWeek))
            return true;
        var previous = start.AddDays(-1);
        return local >= previous && local < previous + window.Duration && DayMatches(window.Days, previous.DayOfWeek);
    }

    private static bool DayMatches(ServiceCampaignDays days, DayOfWeek day)
    {
        if (days == 0 || days.HasFlag(ServiceCampaignDays.All))
            return true;
        var flag = day switch
        {
            DayOfWeek.Sunday => ServiceCampaignDays.Sunday,
            DayOfWeek.Monday => ServiceCampaignDays.Monday,
            DayOfWeek.Tuesday => ServiceCampaignDays.Tuesday,
            DayOfWeek.Wednesday => ServiceCampaignDays.Wednesday,
            DayOfWeek.Thursday => ServiceCampaignDays.Thursday,
            DayOfWeek.Friday => ServiceCampaignDays.Friday,
            _ => ServiceCampaignDays.Saturday
        };
        return days.HasFlag(flag);
    }

    private TimeZoneInfo ResolveZone()
    {
        try { return TimeZoneInfo.FindSystemTimeZoneById(Recipe.TimeZoneId); }
        catch { return TimeZoneInfo.Local; }
    }

    private static void Log(string line)
        => HelperLog.Information(HelperLog.AppIds.Services, VestigiumStatus.Success, HelperLog.Subcategories.Campaign, line);

    internal static ServiceCampaignRecipe Validate(ServiceCampaignRecipe recipe)
    {
        ArgumentNullException.ThrowIfNull(recipe);
        var name = HelperGuard.NotBlank(recipe.Name, nameof(recipe.Name)).Trim();
        HelperGuard.Require(recipe.Windows is { Count: > 0 }, nameof(recipe.Windows), "A campaign needs at least one window. Use Watch for continuous sampling.");
        ServiceHelper.RequireInterval(recipe.SampleInterval);
        HelperGuard.InRange(recipe.MaxMatches, 1, nameof(recipe.MaxMatches));
        HelperGuard.Require(recipe.MaxMatches <= MaxMatchesCap, nameof(recipe.MaxMatches), "MaxMatches cap is 256.");

        var query = string.IsNullOrWhiteSpace(recipe.Query) ? null : recipe.Query.Trim();
        var hasTerm = !string.IsNullOrWhiteSpace(recipe.Match?.Term);
        if (query is null && !hasTerm)
            throw new ArgumentException("A campaign needs Query or Match.Term.", nameof(recipe));
        if (query is not null)
        {
            using var session = KqlHelper.Create(KqlPack.Service);
            var compiled = KqlHelper.Compile(query, session);
            if (!compiled.Ok)
                throw new ArgumentException(compiled.Error?.ToString() ?? "Query compile failed.", nameof(recipe.Query));
        }

        foreach (var window in recipe.Windows)
        {
            HelperGuard.NotBlank(window.Name, nameof(window.Name));
            if (window.Duration < TimeSpan.FromMinutes(1) || window.Duration > TimeSpan.FromHours(24))
                throw new ArgumentOutOfRangeException(nameof(window.Duration), "Window duration must be 1 minute through 24 hours.");
        }

        return new ServiceCampaignRecipe
        {
            Name = Sanitize(name),
            Match = recipe.Match,
            Query = query,
            Scope = recipe.Scope,
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
        if (!string.IsNullOrWhiteSpace(ServiceTestHooks.CampaignRoot))
            return Path.GetFullPath(ServiceTestHooks.CampaignRoot);
        return Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData), "Vestigium", "Services", "Campaigns");
    }

    internal static void WriteRecipe(string folder, ServiceCampaignRecipe recipe)
    {
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "recipe.json");
        File.WriteAllText(path, JsonSerializer.Serialize(recipe, new JsonSerializerOptions { WriteIndented = true }));
    }

    internal static ServiceCampaignRecipe ReadRecipe(string folder)
    {
        var path = Path.Combine(folder, "recipe.json");
        return JsonSerializer.Deserialize<ServiceCampaignRecipe>(File.ReadAllText(path))
            ?? throw new InvalidOperationException("Campaign recipe was empty.");
    }
}
