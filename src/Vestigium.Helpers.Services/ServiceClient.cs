using System.Diagnostics.CodeAnalysis;

namespace Vestigium.Helpers.Services;

/// <summary>Binds inventory and control to a local or remote Service Control Manager.</summary>
public sealed class ServiceClient
{
    internal ServiceClient(string? machine) => Machine = ServiceMachine.Normalize(machine);

    public string? Machine { get; }
    public bool IsLocal => Machine is null;

    public IReadOnlyList<ServiceInfo> List(
        ServiceDetailLevel level = ServiceDetailLevel.Slim,
        ServiceKind kind = ServiceKind.Win32,
        ServiceListScope scope = ServiceListScope.Visible,
        bool joinProcess = false)
    {
        using var _ = ServiceMachine.Push(Machine);
        return ServiceHelper.List(level, kind, scope, joinProcess && IsLocal);
    }

    public IReadOnlyList<ServiceInfo> ListHidden(ServiceDetailLevel level = ServiceDetailLevel.Slim, ServiceKind kind = ServiceKind.All)
        => List(level, kind, ServiceListScope.Hidden);

    public ServiceInfo? Get(string name, ServiceDetailLevel level = ServiceDetailLevel.Full, bool joinProcess = true)
    {
        using var _ = ServiceMachine.Push(Machine);
        return ServiceHelper.Get(name, level, joinProcess && IsLocal);
    }

    public bool TryGet(string name, [NotNullWhen(true)] out ServiceInfo? info)
    {
        using var _ = ServiceMachine.Push(Machine);
        return ServiceHelper.TryGet(name, out info);
    }

    public IReadOnlyList<ServiceInfo> Search(
        string term,
        ServiceSearchMode mode,
        ServiceSearchFields fields = ServiceSearchFields.Default,
        ServiceDetailLevel level = ServiceDetailLevel.Slim,
        ServiceListScope scope = ServiceListScope.Visible,
        int maxResults = ServiceHelper.DefaultMaxSearchResults)
    {
        using var _ = ServiceMachine.Push(Machine);
        return ServiceHelper.Search(term, mode, fields, level, scope, maxResults);
    }

    public IReadOnlyList<ServiceInfo> Search(
        string query,
        ServiceDetailLevel level = ServiceDetailLevel.Slim,
        ServiceListScope scope = ServiceListScope.Visible,
        int maxResults = ServiceHelper.DefaultMaxSearchResults)
    {
        using var _ = ServiceMachine.Push(Machine);
        return ServiceHelper.Search(query, level, scope, maxResults);
    }

    public ServiceTree GetDependencyTree(
        string name,
        ServiceTreeDirection direction = ServiceTreeDirection.DependsOn,
        ServiceDetailLevel level = ServiceDetailLevel.Slim)
    {
        using var _ = ServiceMachine.Push(Machine);
        return ServiceHelper.GetDependencyTree(name, direction, level);
    }

    public ServiceControlResult Start(string name, IReadOnlyList<string>? arguments = null, TimeSpan? timeout = null)
    {
        using var _ = ServiceMachine.Push(Machine);
        return ServiceHelper.Start(name, arguments, timeout);
    }

    public ServiceControlResult Stop(string name, TimeSpan? timeout = null, bool confirmDependents = false)
    {
        using var _ = ServiceMachine.Push(Machine);
        return ServiceHelper.Stop(name, timeout, confirmDependents);
    }

    public ServiceControlResult Restart(string name, TimeSpan? timeout = null, bool confirmDependents = false)
    {
        using var _ = ServiceMachine.Push(Machine);
        return ServiceHelper.Restart(name, timeout, confirmDependents);
    }

    public ServiceControlResult Pause(string name, TimeSpan? timeout = null)
    {
        using var _ = ServiceMachine.Push(Machine);
        return ServiceHelper.Pause(name, timeout);
    }

    public ServiceControlResult Continue(string name, TimeSpan? timeout = null)
    {
        using var _ = ServiceMachine.Push(Machine);
        return ServiceHelper.Continue(name, timeout);
    }

    public ServiceControlResult SetStartType(string name, ServiceStartType startType, bool confirm = false)
    {
        using var _ = ServiceMachine.Push(Machine);
        return ServiceHelper.SetStartType(name, startType, confirm);
    }

    public ServiceControlResult SetLogon(string name, ServiceLogonRequest request)
    {
        using var _ = ServiceMachine.Push(Machine);
        return ServiceHelper.SetLogon(name, request);
    }

    public ServiceControlResult SetRecovery(string name, ServiceRecoveryRequest request)
    {
        using var _ = ServiceMachine.Push(Machine);
        return ServiceHelper.SetRecovery(name, request);
    }

    public ServiceRecoveryInfo? GetRecovery(string name)
    {
        using var _ = ServiceMachine.Push(Machine);
        return ServiceHelper.GetRecovery(name);
    }

    public IServiceWatcher Watch(string name, TimeSpan interval, ServiceWatchFields fields = ServiceWatchFields.Status | ServiceWatchFields.Pid)
    {
        using var _ = ServiceMachine.Push(Machine);
        return ServiceHelper.Watch(name, interval, fields);
    }

    public bool CanConnect(out string? reason) => ServiceMachine.TryConnect(Machine, out reason);
}
