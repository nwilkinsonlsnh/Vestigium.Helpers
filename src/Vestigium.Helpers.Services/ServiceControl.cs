using System.ComponentModel;
using System.ServiceProcess;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Services;

internal static class ServiceControl
{
    public static readonly TimeSpan DefaultTimeout = TimeSpan.FromSeconds(30);

    public static ServiceControlResult Start(string name, IReadOnlyList<string>? arguments, TimeSpan? timeout)
        => Run(name, "Start", controller =>
        {
            if (controller.Status == ServiceControllerStatus.Running)
                return Ok(name, ServiceStatus.Running, "already running");
            if (arguments is { Count: > 0 }) controller.Start([.. arguments]); else controller.Start();
            controller.WaitForStatus(ServiceControllerStatus.Running, timeout ?? DefaultTimeout);
            return Ok(name, ServiceStatus.Running, null);
        });

    public static ServiceControlResult Stop(string name, TimeSpan? timeout, bool confirmDependents)
        => Run(name, "Stop", controller =>
        {
            if (controller.Status == ServiceControllerStatus.Stopped)
                return Ok(name, ServiceStatus.Stopped, "already stopped");
            if (controller.DependentServices.Length > 0 && !confirmDependents)
                return new ServiceControlResult(name, ServiceControlStatus.HasDependents, null, "confirmDependents=false");
            controller.Stop();
            controller.WaitForStatus(ServiceControllerStatus.Stopped, timeout ?? DefaultTimeout);
            return Ok(name, ServiceStatus.Stopped, null);
        });

    public static ServiceControlResult Pause(string name, TimeSpan? timeout)
        => Run(name, "Pause", controller =>
        {
            if (!controller.CanPauseAndContinue)
                return new ServiceControlResult(name, ServiceControlStatus.Unsupported, null, "service does not accept pause");
            controller.Pause();
            controller.WaitForStatus(ServiceControllerStatus.Paused, timeout ?? DefaultTimeout);
            return Ok(name, ServiceStatus.Paused, null);
        });

    public static ServiceControlResult Continue(string name, TimeSpan? timeout)
        => Run(name, "Continue", controller =>
        {
            if (!controller.CanPauseAndContinue)
                return new ServiceControlResult(name, ServiceControlStatus.Unsupported, null, "service does not accept continue");
            controller.Continue();
            controller.WaitForStatus(ServiceControllerStatus.Running, timeout ?? DefaultTimeout);
            return Ok(name, ServiceStatus.Running, null);
        });

    public static ServiceControlResult Restart(string name, TimeSpan? timeout, bool confirmDependents)
    {
        var stopped = Stop(name, timeout, confirmDependents);
        if (stopped.Status is not ServiceControlStatus.Ok && stopped.ResultingState is not ServiceStatus.Stopped)
            return stopped;
        return Start(name, null, timeout);
    }

    public static ServiceControlResult SetStartType(string name, ServiceStartType startType, bool confirm)
    {
        if (!confirm)
            return Fail(name, ServiceControlStatus.Denied, "confirm=false");
        var delayed = startType == ServiceStartType.AutomaticDelayed;
        var scmType = delayed ? ServiceStartType.Automatic : startType;
        var handle = ServiceSnapshotter.Open(name, ServiceNative.ServiceChangeConfig | ServiceNative.ServiceQueryConfig);
        if (handle == 0)
            return Fail(name, ServiceControlStatus.Denied, "OpenService");
        try
        {
            if (!ServiceNative.ChangeServiceConfig(handle, ServiceNative.ServiceNoChange, (uint)scmType, ServiceNative.ServiceNoChange, null, null, 0, null, null, null, null))
                return Fail(name, ServiceControlStatus.Denied, "ChangeServiceConfig");
            var info = new ServiceNative.DelayedAutoStartInfo { DelayedAutostart = delayed };
            var ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(System.Runtime.InteropServices.Marshal.SizeOf<ServiceNative.DelayedAutoStartInfo>());
            try
            {
                System.Runtime.InteropServices.Marshal.StructureToPtr(info, ptr, false);
                ServiceNative.ChangeServiceConfig2(handle, ServiceNative.ServiceConfigDelayedAutoStart, ptr);
            }
            finally { System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr); }
            HelperLog.Information(HelperLog.AppIds.Services, VestigiumStatus.Success, HelperLog.Subcategories.Start, $"SetStartType {name} {startType}");
            var snap = ServiceSnapshotter.CaptureName(name, ServiceDetailLevel.Slim, joinProcess: false);
            return Ok(name, snap?.Status, null);
        }
        finally { ServiceNative.CloseServiceHandle(handle); }
    }

    private static ServiceControlResult Run(string name, string verb, Func<ServiceController, ServiceControlResult> body)
    {
        try
        {
            using var controller = new ServiceController(name);
            var result = body(controller);
            if (result.Status == ServiceControlStatus.Ok)
                HelperLog.Information(HelperLog.AppIds.Services, VestigiumStatus.Success, HelperLog.Subcategories.Start, $"{verb} {name} ok");
            return result;
        }
        catch (InvalidOperationException ex) { return Fail(name, Map(ex), ex.Message); }
        catch (System.ServiceProcess.TimeoutException) { return Fail(name, ServiceControlStatus.Timeout, "wait expired"); }
        catch (UnauthorizedAccessException ex) { return Fail(name, ServiceControlStatus.Denied, ex.Message); }
        catch (Win32Exception ex) { return Fail(name, ServiceControlStatus.Denied, ex.Message); }
        catch (Exception ex) { return Fail(name, ServiceControlStatus.Failed, ex.Message); }
    }

    private static ServiceControlResult Ok(string name, ServiceStatus? state, string? reason) => new(name, ServiceControlStatus.Ok, state, reason);

    private static ServiceControlResult Fail(string name, ServiceControlStatus status, string? reason)
    {
        HelperLog.Error(HelperLog.AppIds.Services, VestigiumStatus.Failed, HelperLog.Subcategories.Start, $"{status} {name} {reason}");
        return new ServiceControlResult(name, status, null, reason);
    }

    private static ServiceControlStatus Map(InvalidOperationException ex)
    {
        var text = ex.Message ?? string.Empty;
        if (text.Contains("cannot be found", StringComparison.OrdinalIgnoreCase) || text.Contains("does not exist", StringComparison.OrdinalIgnoreCase))
            return ServiceControlStatus.NotFound;
        if (text.Contains("disabled", StringComparison.OrdinalIgnoreCase))
            return ServiceControlStatus.InvalidState;
        if (text.Contains("access", StringComparison.OrdinalIgnoreCase))
            return ServiceControlStatus.Denied;
        return ServiceControlStatus.Failed;
    }
}
