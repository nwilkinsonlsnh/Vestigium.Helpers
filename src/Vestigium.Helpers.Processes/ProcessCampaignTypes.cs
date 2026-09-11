namespace Vestigium.Helpers.Processes;

[Flags]
public enum ProcessCampaignDays
{
    Sunday = 1,
    Monday = 2,
    Tuesday = 4,
    Wednesday = 8,
    Thursday = 16,
    Friday = 32,
    Saturday = 64,
    Weekdays = Monday | Tuesday | Wednesday | Thursday | Friday,
    Weekend = Saturday | Sunday,
    All = Sunday | Monday | Tuesday | Wednesday | Thursday | Friday | Saturday
}

public readonly record struct ProcessCampaignWindow(
    string Name,
    TimeOnly StartLocal,
    TimeSpan Duration,
    ProcessCampaignDays Days);

public sealed class ProcessCampaignRecipe
{
    public required string Name { get; init; }
    public ProcessSearchRequest? Match { get; init; }
    public string? Query { get; init; }
    public ProcessWatchFields Fields { get; init; } = ProcessWatchFields.All;
    public bool IncludeSystemCounters { get; init; } = true;
    public TimeSpan SampleInterval { get; init; } = TimeSpan.FromSeconds(1);
    public IReadOnlyList<ProcessCampaignWindow> Windows { get; init; } = [];
    public string TimeZoneId { get; init; } = TimeZoneInfo.Local.Id;
    public int MaxMatches { get; init; } = 64;
}

public enum ProcessCampaignState
{
    Idle = 0,
    Waiting = 1,
    Sampling = 2,
    Stopped = 3
}

public sealed class ProcessCampaignTick
{
    public required DateTimeOffset Timestamp { get; init; }
    public required IReadOnlyList<string> OpenWindows { get; init; }
    public required IReadOnlyList<ProcessInfo> Processes { get; init; }
    public SystemCounters? System { get; init; }
    public bool Truncated { get; init; }
}

public sealed class ProcessCampaignWindowEvent : EventArgs
{
    public required string WindowName { get; init; }
    public required bool Open { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
