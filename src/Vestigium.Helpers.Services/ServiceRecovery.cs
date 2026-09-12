using System.Runtime.InteropServices;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Services;

internal static class ServiceRecovery
{
    public static ServiceControlResult Set(string name, ServiceRecoveryRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.Confirm)
            return new ServiceControlResult(name, ServiceControlStatus.Denied, null, "confirm=false");
        if (ServiceControl.IsProtected(name))
            return new ServiceControlResult(name, ServiceControlStatus.Denied, null, "protected service");
        if (ServiceSnapshotter.CaptureName(name, ServiceDetailLevel.Identity, joinProcess: false) is null)
            return new ServiceControlResult(name, ServiceControlStatus.NotFound, null, "service gone");
        if (request.ResetPeriod < TimeSpan.Zero || request.ActionDelay < TimeSpan.Zero)
            return new ServiceControlResult(name, ServiceControlStatus.InvalidState, null, "ResetPeriod and ActionDelay cannot be negative");
        if (!IsAction(request.FirstFailure) || !IsAction(request.SecondFailure) || !IsAction(request.SubsequentFailures))
            return new ServiceControlResult(name, ServiceControlStatus.InvalidState, null, "unknown failure action");
        if (NeedsCommand(request) && string.IsNullOrWhiteSpace(request.Command))
            return new ServiceControlResult(name, ServiceControlStatus.InvalidState, null, "RunCommand requires Command");

        var handle = ServiceSnapshotter.Open(name, ServiceNative.ServiceChangeConfig | ServiceNative.ServiceQueryConfig);
        if (handle == 0)
            return new ServiceControlResult(name, ServiceControlStatus.Denied, null, "OpenService");

        nint reboot = 0, command = 0, actions = 0, block = 0;
        try
        {
            var delay = (uint)Math.Min(uint.MaxValue, Math.Max(0, request.ActionDelay.TotalMilliseconds));
            var rows = new[]
            {
                new ServiceNative.ScAction { Type = (uint)request.FirstFailure, Delay = delay },
                new ServiceNative.ScAction { Type = (uint)request.SecondFailure, Delay = delay },
                new ServiceNative.ScAction { Type = (uint)request.SubsequentFailures, Delay = delay }
            };
            var stride = Marshal.SizeOf<ServiceNative.ScAction>();
            actions = Marshal.AllocHGlobal(stride * rows.Length);
            for (var i = 0; i < rows.Length; i++)
                Marshal.StructureToPtr(rows[i], actions + (i * stride), false);
            if (!string.IsNullOrWhiteSpace(request.RebootMessage))
                reboot = Marshal.StringToHGlobalUni(request.RebootMessage);
            if (!string.IsNullOrWhiteSpace(request.Command))
                command = Marshal.StringToHGlobalUni(request.Command);
            var info = new ServiceNative.FailureActions
            {
                ResetPeriod = (uint)Math.Min(uint.MaxValue, Math.Max(0, request.ResetPeriod.TotalSeconds)),
                RebootMsg = reboot,
                Command = command,
                Count = (uint)rows.Length,
                Actions = actions
            };
            block = Marshal.AllocHGlobal(Marshal.SizeOf<ServiceNative.FailureActions>());
            Marshal.StructureToPtr(info, block, false);
            if (!ServiceNative.ChangeServiceConfig2(handle, ServiceNative.ServiceConfigFailureActions, block))
                return new ServiceControlResult(name, ServiceControlStatus.Denied, null, "ChangeServiceConfig2");

            HelperLog.Information(
                HelperLog.AppIds.Services,
                VestigiumStatus.Success,
                HelperLog.Subcategories.Start,
                $"SetRecovery {name} first={request.FirstFailure} second={request.SecondFailure} subsequent={request.SubsequentFailures} reset={request.ResetPeriod}");
            var snap = ServiceSnapshotter.CaptureName(name, ServiceDetailLevel.Slim, joinProcess: false);
            return new ServiceControlResult(name, ServiceControlStatus.Ok, snap?.Status, null);
        }
        finally
        {
            if (reboot != 0) Marshal.FreeHGlobal(reboot);
            if (command != 0) Marshal.FreeHGlobal(command);
            if (actions != 0) Marshal.FreeHGlobal(actions);
            if (block != 0) Marshal.FreeHGlobal(block);
            ServiceNative.CloseServiceHandle(handle);
        }
    }

    public static ServiceRecoveryInfo? Get(string name)
    {
        var row = ServiceSnapshotter.CaptureName(name, ServiceDetailLevel.Full, joinProcess: false);
        if (row is null)
            return null;
        return new ServiceRecoveryInfo
        {
            Name = row.Name,
            ResetPeriod = row.FailureResetPeriod,
            Command = row.FailureCommand,
            RebootMessage = row.FailureRebootMessage,
            Actions = row.FailureActions
        };
    }

    private static bool IsAction(ServiceFailureActionKind kind)
        => kind is ServiceFailureActionKind.None
            or ServiceFailureActionKind.Restart
            or ServiceFailureActionKind.Reboot
            or ServiceFailureActionKind.RunCommand;

    private static bool NeedsCommand(ServiceRecoveryRequest request)
        => request.FirstFailure == ServiceFailureActionKind.RunCommand
            || request.SecondFailure == ServiceFailureActionKind.RunCommand
            || request.SubsequentFailures == ServiceFailureActionKind.RunCommand;
}
