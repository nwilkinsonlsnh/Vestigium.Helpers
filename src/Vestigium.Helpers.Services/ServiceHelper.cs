using System.Diagnostics.CodeAnalysis;
using Vestigium.Helpers;
using Vestigium.Helpers.Kql;
using Vestigium.Logging;

namespace Vestigium.Helpers.Services;

/// <summary>Service Control Manager helpers.</summary>
public static class ServiceHelper
{
    public const int DefaultMaxSearchResults = 256;
    public const int MaxSearchResultsCap = 256;
    public static readonly TimeSpan MinWatchInterval = TimeSpan.FromMilliseconds(250);
    public static readonly TimeSpan MaxWatchInterval = TimeSpan.FromSeconds(60);
    public static readonly TimeSpan DefaultWatchInterval = TimeSpan.FromSeconds(1);

    public static string Identity => "Vestigium.Helpers.Services";

    public static string Probe()
    {
        var app = HelperLog.AppIds.Services;
        HelperLog.Information(app, VestigiumStatus.Pending, HelperLog.Subcategories.Inventory, "Enumerating service-control surface.");
        var rows = List(ServiceDetailLevel.Identity);
        TryGet("EventLog", out _);
        HelperLog.Information(app, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"Service probe complete. Identity={Identity} n={rows.Count}");
        return Identity;
    }

    public static IReadOnlyList<ServiceInfo> List(
        ServiceDetailLevel level = ServiceDetailLevel.Slim,
        ServiceKind kind = ServiceKind.Win32,
        ServiceListScope scope = ServiceListScope.Visible,
        bool joinProcess = false)
    {
        var app = HelperLog.AppIds.Services;
        using var scopeLog = HelperLog.Begin(app, HelperLog.Subcategories.Inventory, "List", $"{level}/{kind}/{scope}");
        var rows = ServiceSnapshotter.Capture(level, kind, scope, joinProcess);
        HelperLog.Information(app, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"List n={rows.Count} scope={scope}");
        return rows;
    }

    public static IReadOnlyList<ServiceInfo> ListHidden(
        ServiceDetailLevel level = ServiceDetailLevel.Slim,
        ServiceKind kind = ServiceKind.All)
        => List(level, kind, ServiceListScope.Hidden);

    public static ServiceInfo? Get(string name, ServiceDetailLevel level = ServiceDetailLevel.Full, bool joinProcess = true)
    {
        var key = HelperGuard.NotBlank(name, nameof(name));
        var app = HelperLog.AppIds.Services;
        using var scope = HelperLog.Begin(app, HelperLog.Subcategories.Inventory, "Get", key);
        var row = ServiceSnapshotter.CaptureName(key, level, joinProcess);
        HelperLog.Information(app, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, row is null ? $"Get {key} gone" : $"Get {key} status={row.Status}");
        return row;
    }

    public static bool TryGet(string name, [NotNullWhen(true)] out ServiceInfo? info)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            info = null;
            return false;
        }

        info = ServiceSnapshotter.CaptureName(name.Trim(), ServiceDetailLevel.Full, joinProcess: false);
        return info is not null;
    }

    public static IReadOnlyList<ServiceInfo> Search(
        string term,
        ServiceSearchMode mode,
        ServiceSearchFields fields = ServiceSearchFields.Default,
        ServiceDetailLevel level = ServiceDetailLevel.Slim,
        ServiceListScope scope = ServiceListScope.Visible,
        int maxResults = DefaultMaxSearchResults)
    {
        var needle = HelperGuard.NotBlank(term, nameof(term)).Trim();
        HelperGuard.InRange(maxResults, 1, nameof(maxResults));
        HelperGuard.Require(maxResults <= MaxSearchResultsCap, nameof(maxResults), "MaxMatches cap is 256.");
        if (fields == 0)
            fields = ServiceSearchFields.Default;

        var hits = new List<ServiceInfo>();
        foreach (var row in List(level, ServiceKind.All, scope))
        {
            if (Matches(row, needle, mode, fields))
                hits.Add(row);
            if (hits.Count >= maxResults)
                break;
        }

        HelperLog.Information(HelperLog.AppIds.Services, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"Search mode={mode} hits={hits.Count}");
        return hits;
    }

    public static IReadOnlyList<ServiceInfo> Search(
        string query,
        ServiceDetailLevel level = ServiceDetailLevel.Slim,
        ServiceListScope scope = ServiceListScope.Visible,
        int maxResults = DefaultMaxSearchResults)
    {
        var text = HelperGuard.NotBlank(query, nameof(query));
        HelperGuard.InRange(maxResults, 1, nameof(maxResults));
        HelperGuard.Require(maxResults <= MaxSearchResultsCap, nameof(maxResults), "MaxMatches cap is 256.");

        using var session = KqlHelper.Create(KqlPack.Service);
        var compiled = KqlHelper.Compile(text, session);
        if (!compiled.Ok || compiled.Query is null)
            throw new ArgumentException(compiled.Error?.ToString() ?? "invalid query", nameof(query));

        var hits = new List<ServiceInfo>();
        foreach (var row in List(level, ServiceKind.All, scope))
        {
            if (compiled.Query.Matches(new ServiceKqlRow(session, row)))
                hits.Add(row);
            if (hits.Count >= maxResults)
                break;
        }

        return hits;
    }

    public static ServiceTree GetDependencyTree(
        string name,
        ServiceTreeDirection direction = ServiceTreeDirection.DependsOn,
        ServiceDetailLevel level = ServiceDetailLevel.Slim)
    {
        var key = HelperGuard.NotBlank(name, nameof(name));
        return ServiceTreeWalker.Build(key, direction, level)
            ?? throw new InvalidOperationException($"Service {key} is gone.");
    }

    public static IReadOnlyList<ServiceInfo> GetDependsOn(string name)
        => Get(name, ServiceDetailLevel.Full, joinProcess: false)?.DependsOn
            .Select(n => Get(n, ServiceDetailLevel.Identity, joinProcess: false))
            .Where(s => s is not null)
            .Cast<ServiceInfo>()
            .ToArray() ?? [];

    public static IReadOnlyList<ServiceInfo> GetDependedBy(string name)
        => Get(name, ServiceDetailLevel.Full, joinProcess: false)?.DependedBy
            .Select(n => Get(n, ServiceDetailLevel.Identity, joinProcess: false))
            .Where(s => s is not null)
            .Cast<ServiceInfo>()
            .ToArray() ?? [];

    public static ServiceControlResult Start(string name, IReadOnlyList<string>? arguments = null, TimeSpan? timeout = null)
        => ServiceControl.Start(HelperGuard.NotBlank(name, nameof(name)), arguments, timeout);

    public static ServiceControlResult Stop(string name, TimeSpan? timeout = null, bool confirmDependents = false)
        => ServiceControl.Stop(HelperGuard.NotBlank(name, nameof(name)), timeout, confirmDependents);

    public static ServiceControlResult Pause(string name, TimeSpan? timeout = null)
        => ServiceControl.Pause(HelperGuard.NotBlank(name, nameof(name)), timeout);

    public static ServiceControlResult Continue(string name, TimeSpan? timeout = null)
        => ServiceControl.Continue(HelperGuard.NotBlank(name, nameof(name)), timeout);

    public static ServiceControlResult Restart(string name, TimeSpan? timeout = null, bool confirmDependents = false)
        => ServiceControl.Restart(HelperGuard.NotBlank(name, nameof(name)), timeout, confirmDependents);

    public static ServiceControlResult SetStartType(string name, ServiceStartType startType, bool confirm = false)
        => ServiceControl.SetStartType(HelperGuard.NotBlank(name, nameof(name)), startType, confirm);

    public static ServiceControlResult SetLogon(string name, ServiceLogonRequest request)
        => ServiceLogon.Set(HelperGuard.NotBlank(name, nameof(name)), request);

    public static ServiceAccountRightInfo QueryLogonRights(string account)
        => ServiceLogon.QueryRights(account);

    public static ServiceAccountRightInfo GrantLogonRights(string account, ServiceGrantLogonRight rights)
        => ServiceLogon.Grant(account, rights);

    public static ServiceControlResult SetRecovery(string name, ServiceRecoveryRequest request)
        => ServiceRecovery.Set(HelperGuard.NotBlank(name, nameof(name)), request);

    public static IServiceWatcher Watch(string name, TimeSpan interval, ServiceWatchFields fields = ServiceWatchFields.Status | ServiceWatchFields.Pid)
    {
        var key = HelperGuard.NotBlank(name, nameof(name));
        return new ServiceWatcher(key, query: null, RequireInterval(interval), fields);
    }

    public static IServiceWatcher WatchQuery(string query, TimeSpan interval, ServiceWatchFields fields = ServiceWatchFields.Status | ServiceWatchFields.Pid)
    {
        var text = HelperGuard.NotBlank(query, nameof(query));
        return new ServiceWatcher(name: null, text, RequireInterval(interval), fields);
    }

    internal static TimeSpan RequireInterval(TimeSpan interval)
    {
        if (interval < MinWatchInterval || interval > MaxWatchInterval)
        {
            HelperLog.Reject($"interval={interval}");
            throw new ArgumentOutOfRangeException(nameof(interval), "Watcher interval must be 250 ms through 60 s.");
        }

        return interval;
    }

    private static bool Matches(ServiceInfo row, string term, ServiceSearchMode mode, ServiceSearchFields fields)
    {
        if (fields.HasFlag(ServiceSearchFields.Name) && Compare(row.Name, term, mode))
            return true;
        if (fields.HasFlag(ServiceSearchFields.DisplayName) && Compare(row.DisplayName, term, mode))
            return true;
        if (fields.HasFlag(ServiceSearchFields.Description) && Compare(row.Description, term, mode))
            return true;
        if (fields.HasFlag(ServiceSearchFields.ImagePath) && Compare(row.ImagePath, term, mode))
            return true;
        if (fields.HasFlag(ServiceSearchFields.Account) && Compare(row.Account, term, mode))
            return true;
        return false;
    }

    private static bool Compare(string? value, string term, ServiceSearchMode mode)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        return mode switch
        {
            ServiceSearchMode.StartsWith => value.StartsWith(term, StringComparison.OrdinalIgnoreCase),
            ServiceSearchMode.EndsWith => value.EndsWith(term, StringComparison.OrdinalIgnoreCase),
            ServiceSearchMode.Contains => value.Contains(term, StringComparison.OrdinalIgnoreCase),
            _ => false
        };
    }
}
