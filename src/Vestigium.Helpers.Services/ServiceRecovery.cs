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

        var handle = ServiceSnapshotter.Open(name, ServiceNative.ServiceChangeConfig | ServiceNative.ServiceQueryConfig);
        if (handle == 0)
            return new ServiceControlResult(name, ServiceControlStatus.Denied, null, "OpenService");

        nint reboot = 0, command = 0, actions = 0, block = 0;
        try
        {
            var rows = new[]
            {
                new ServiceNative.ScAction { Type = (uint)request.FirstFailure, Delay = (uint)Math.Max(0, request.ActionDelay.TotalMilliseconds) },
                new ServiceNative.ScAction { Type = (uint)request.SecondFailure, Delay = (uint)Math.Max(0, request.ActionDelay.TotalMilliseconds) },
                new ServiceNative.ScAction { Type = (uint)request.SubsequentFailures, Delay = (uint)Math.Max(0, request.ActionDelay.TotalMilliseconds) }
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
                ResetPeriod = (uint)Math.Max(0, request.ResetPeriod.TotalSeconds),
                RebootMsg = reboot,
                Command = command,
                Count = (uint)rows.Length,
                Actions = actions
            };
            block = Marshal.AllocHGlobal(Marshal.SizeOf<ServiceNative.FailureActions>());
            Marshal.StructureToPtr(info, block, false);
            if (!ServiceNative.ChangeServiceConfig2(handle, ServiceNative.ServiceConfigFailureActions, block))
                return new ServiceControlResult(name, ServiceControlStatus.Denied, null, "ChangeServiceConfig2");
            HelperLog.Information(HelperLog.AppIds.Services, VestigiumStatus.Success, HelperLog.Subcategories.Start, $"SetRecovery {name}");
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
}
