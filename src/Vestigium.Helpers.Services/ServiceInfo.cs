using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Services;

public sealed class ServiceInfo
{
    public required string Name { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; set; }
    public ServiceKind Kind { get; init; }
    public ServiceTypeFlags ServiceType { get; init; }
    public bool SharedProcess { get; init; }
    public bool IsHidden { get; init; }
    public string? Comment { get; set; }

    public ServiceStatus Status { get; init; }
    public int? Pid { get; init; }
    public int? Win32ExitCode { get; init; }
    public int? ServiceSpecificExitCode { get; init; }
    public int? Checkpoint { get; init; }
    public TimeSpan? WaitHint { get; init; }
    public ServiceControls ControlsAccepted { get; init; }
    public bool CanPauseAndContinue { get; init; }
    public ProcessInfo? Process { get; set; }

    public ServiceStartType? StartType { get; set; }
    public bool? DelayedAutoStart { get; set; }
    public bool? TriggerStart { get; set; }
    public ServiceErrorControl? ErrorControl { get; set; }
    public string? ImagePath { get; set; }
    public string? LoadOrderGroup { get; set; }
    public int? TagId { get; set; }
    public string? Account { get; set; }
    public bool? DesktopInteract { get; set; }
    public ServiceSidType? SidType { get; set; }
    public IReadOnlyList<string>? RequiredPrivileges { get; set; }
    public ServiceLaunchProtected? LaunchProtected { get; set; }
    public TimeSpan? PreshutdownTimeout { get; set; }

    public IReadOnlyList<string> DependsOn { get; set; } = [];
    public IReadOnlyList<string> DependedBy { get; set; } = [];
    public bool AmbiguousDependency { get; set; }

    public TimeSpan? FailureResetPeriod { get; set; }
    public string? FailureRebootMessage { get; set; }
    public string? FailureCommand { get; set; }
    public IReadOnlyList<ServiceFailureAction> FailureActions { get; set; } = [];
    public IReadOnlyList<ServiceTriggerInfo> Triggers { get; set; } = [];

    public IReadOnlyList<ServiceFieldAvailability> Availability { get; set; } = [];
}

public readonly record struct ServiceFieldAvailability(ServiceField Field, string State, string? Reason);

public readonly record struct ServiceFailureAction(ServiceFailureActionKind Kind, TimeSpan Delay);

public readonly record struct ServiceTriggerInfo(int Type, int Action, Guid? Subtype, string? Data);

public sealed class ServiceTree
{
    public required ServiceInfo Root { get; init; }
    public IReadOnlyList<ServiceTree> Children { get; init; } = [];

    public IReadOnlyList<ServiceInfo> Flatten()
    {
        var rows = new List<ServiceInfo>();
        Walk(this, rows);
        return rows;
    }

    private static void Walk(ServiceTree node, List<ServiceInfo> rows)
    {
        rows.Add(node.Root);
        foreach (var child in node.Children)
            Walk(child, rows);
    }
}

public readonly record struct ServiceControlResult(
    string Name,
    ServiceControlStatus Status,
    ServiceStatus? ResultingState,
    string? Reason);

public sealed class ServiceLogonRequest
{
    public ServiceLogonKind Kind { get; init; } = ServiceLogonKind.LocalSystem;
    public string? Account { get; init; }
    public string? Password { get; init; }
    public bool InteractWithDesktop { get; init; }
    public ServiceGrantLogonRight GrantLogonRight { get; init; } = ServiceGrantLogonRight.None;
    public bool Confirm { get; init; }
}

public sealed class ServiceRecoveryRequest
{
    public ServiceFailureActionKind FirstFailure { get; init; }
    public ServiceFailureActionKind SecondFailure { get; init; }
    public ServiceFailureActionKind SubsequentFailures { get; init; }
    public TimeSpan ActionDelay { get; init; } = TimeSpan.FromMinutes(1);
    public TimeSpan ResetPeriod { get; init; } = TimeSpan.FromDays(1);
    public string? Command { get; init; }
    public string? RebootMessage { get; init; }
    public bool Confirm { get; init; }
}

public readonly record struct ServiceAccountRightInfo(
    string Account,
    bool HasServiceLogon,
    bool HasBatchLogon,
    string? Reason);
