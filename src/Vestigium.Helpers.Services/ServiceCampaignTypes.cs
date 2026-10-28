namespace Vestigium.Helpers.Services;

[Flags]
public enum ServiceCampaignDays
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

public readonly record struct ServiceCampaignWindow(
    string Name,
    TimeOnly StartLocal,
    TimeSpan Duration,
    ServiceCampaignDays Days);

public sealed class ServiceSearchRequest
{
    public required string Term { get; init; }
    public ServiceSearchMode Mode { get; init; } = ServiceSearchMode.Contains;
    public ServiceSearchFields Fields { get; init; } = ServiceSearchFields.Default;
}

public sealed class ServiceCampaignRecipe
{
    public required string Name { get; init; }
    public ServiceSearchRequest? Match { get; init; }
    public string? Query { get; init; }
    public ServiceListScope Scope { get; init; } = ServiceListScope.Visible;
    public TimeSpan SampleInterval { get; init; } = TimeSpan.FromSeconds(1);
    public IReadOnlyList<ServiceCampaignWindow> Windows { get; init; } = [];
    public string TimeZoneId { get; init; } = TimeZoneInfo.Local.Id;
    public int MaxMatches { get; init; } = 64;
}

public enum ServiceCampaignState
{
    Idle = 0,
    Waiting = 1,
    Sampling = 2,
    Stopped = 3
}

public sealed class ServiceCampaignTick
{
    public required DateTimeOffset Timestamp { get; init; }
    public required IReadOnlyList<string> OpenWindows { get; init; }
    public required IReadOnlyList<ServiceInfo> Services { get; init; }
    public bool Truncated { get; init; }
}

public sealed class ServiceCampaignWindowEvent : EventArgs
{
    public required string WindowName { get; init; }
    public required bool Open { get; init; }
    public required DateTimeOffset Timestamp { get; init; }
}
