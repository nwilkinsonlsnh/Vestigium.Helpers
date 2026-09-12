using System.Runtime.InteropServices;
using System.ServiceProcess;

namespace Vestigium.Helpers.Services;

internal static partial class ServiceSnapshotter
{
    private static void FillConfig(string name, ServiceInfo info, List<ServiceFieldAvailability> availability)
    {
        var service = Open(name, ServiceNative.ServiceQueryConfig);
        if (service == 0)
        {
            availability.Add(new ServiceFieldAvailability(ServiceField.StartType, "Denied", "OpenService"));
            availability.Add(new ServiceFieldAvailability(ServiceField.ImagePath, "Denied", "OpenService"));
            availability.Add(new ServiceFieldAvailability(ServiceField.Account, "Denied", "OpenService"));
            return;
        }

        try
        {
            QueryConfig(service, 8 * 1024, out var needed);
            var size = Math.Max(needed, 8 * 1024);
            var buffer = Marshal.AllocHGlobal(size);
            try
            {
                if (!ServiceNative.QueryServiceConfig(service, buffer, size, out _))
                {
                    availability.Add(new ServiceFieldAvailability(ServiceField.StartType, "Denied", "QueryServiceConfig"));
                    availability.Add(new ServiceFieldAvailability(ServiceField.ImagePath, "Denied", "QueryServiceConfig"));
                    availability.Add(new ServiceFieldAvailability(ServiceField.Account, "Denied", "QueryServiceConfig"));
                    return;
                }

                var cfg = Marshal.PtrToStructure<ServiceNative.QueryServiceConfigData>(buffer);
                info.StartType = (ServiceStartType)cfg.StartType;
                info.ErrorControl = (ServiceErrorControl)cfg.ErrorControl;
                info.ImagePath = ServiceNative.PtrToString(cfg.BinaryPathName);
                info.LoadOrderGroup = ServiceNative.PtrToString(cfg.LoadOrderGroup);
                info.TagId = cfg.TagId == 0 ? null : (int)cfg.TagId;
                info.Account = NormalizeAccount(ServiceNative.PtrToString(cfg.ServiceStartName));
                info.DesktopInteract = ((ServiceTypeFlags)cfg.ServiceType).HasFlag(ServiceTypeFlags.InteractiveProcess);
                info.DependsOn = ParseMulti(ServiceNative.PtrToString(cfg.Dependencies));
            }
            finally
            {
                Marshal.FreeHGlobal(buffer);
            }

            info.DelayedAutoStart = QueryDelayed(service);
            if (info.DelayedAutoStart == true && info.StartType == ServiceStartType.Automatic)
                info.StartType = ServiceStartType.AutomaticDelayed;

            FillSidAndPrivileges(service, info);
        }
        finally
        {
            ServiceNative.CloseServiceHandle(service);
        }
    }

    private static void FillSidAndPrivileges(nint service, ServiceInfo info)
    {
        var sidBuf = Marshal.AllocHGlobal(8);
        try
        {
            if (ServiceNative.QueryServiceConfig2(service, ServiceNative.ServiceConfigServiceSidInfo, sidBuf, 8, out _))
                info.SidType = (ServiceSidType)Marshal.ReadInt32(sidBuf);
        }
        finally { Marshal.FreeHGlobal(sidBuf); }

        var privBuf = Marshal.AllocHGlobal(8 * 1024);
        try
        {
            if (ServiceNative.QueryServiceConfig2(service, ServiceNative.ServiceConfigRequiredPrivileges, privBuf, 8 * 1024, out _))
            {
                var ptr = Marshal.ReadIntPtr(privBuf);
                info.RequiredPrivileges = ParseMulti(ServiceNative.PtrToString(ptr));
            }
        }
        finally { Marshal.FreeHGlobal(privBuf); }

        var preBuf = Marshal.AllocHGlobal(8);
        try
        {
            if (ServiceNative.QueryServiceConfig2(service, ServiceNative.ServiceConfigPreshutdown, preBuf, 8, out _))
                info.PreshutdownTimeout = TimeSpan.FromMilliseconds(Marshal.ReadInt32(preBuf));
        }
        finally { Marshal.FreeHGlobal(preBuf); }

        var launchBuf = Marshal.AllocHGlobal(8);
        try
        {
            if (ServiceNative.QueryServiceConfig2(service, ServiceNative.ServiceConfigLaunchProtected, launchBuf, 8, out _))
                info.LaunchProtected = (ServiceLaunchProtected)Marshal.ReadInt32(launchBuf);
        }
        finally { Marshal.FreeHGlobal(launchBuf); }
    }

    private static void FillDescription(string name, ServiceInfo info, List<ServiceFieldAvailability> availability)
    {
        var service = Open(name, ServiceNative.ServiceQueryConfig);
        if (service == 0)
            return;
        try
        {
            var buffer = Marshal.AllocHGlobal(8 * 1024);
            try
            {
                if (!ServiceNative.QueryServiceConfig2(service, ServiceNative.ServiceConfigDescription, buffer, 8 * 1024, out _))
                {
                    availability.Add(new ServiceFieldAvailability(ServiceField.Description, "Denied", "QueryServiceConfig2"));
                    return;
                }
                info.Description = ServiceNative.PtrToString(Marshal.ReadIntPtr(buffer));
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
        finally { ServiceNative.CloseServiceHandle(service); }
    }

    private static void FillFailure(string name, ServiceInfo info, List<ServiceFieldAvailability> availability)
    {
        var service = Open(name, ServiceNative.ServiceQueryConfig);
        if (service == 0)
            return;
        try
        {
            var buffer = Marshal.AllocHGlobal(16 * 1024);
            try
            {
                if (!ServiceNative.QueryServiceConfig2(service, ServiceNative.ServiceConfigFailureActions, buffer, 16 * 1024, out _))
                {
                    availability.Add(new ServiceFieldAvailability(ServiceField.FailureActions, "Denied", "QueryServiceConfig2"));
                    return;
                }
                var actions = Marshal.PtrToStructure<ServiceNative.FailureActions>(buffer);
                info.FailureResetPeriod = TimeSpan.FromSeconds(actions.ResetPeriod);
                info.FailureRebootMessage = ServiceNative.PtrToString(actions.RebootMsg);
                info.FailureCommand = ServiceNative.PtrToString(actions.Command);
                var list = new List<ServiceFailureAction>();
                if (actions.Actions != 0 && actions.Count > 0 && actions.Count < 16)
                {
                    var stride = Marshal.SizeOf<ServiceNative.ScAction>();
                    for (var i = 0; i < actions.Count; i++)
                    {
                        var row = Marshal.PtrToStructure<ServiceNative.ScAction>(actions.Actions + (i * stride));
                        list.Add(new ServiceFailureAction((ServiceFailureActionKind)row.Type, TimeSpan.FromMilliseconds(row.Delay)));
                    }
                }
                info.FailureActions = list;
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
        finally { ServiceNative.CloseServiceHandle(service); }
    }

    private static void FillDepends(ServiceController? controller, ServiceInfo info, List<ServiceFieldAvailability> availability)
    {
        if (controller is null) return;
        try
        {
            info.DependsOn = controller.ServicesDependedOn.Select(s => s.ServiceName).ToArray();
            info.DependedBy = controller.DependentServices.Select(s => s.ServiceName).ToArray();
        }
        catch
        {
            availability.Add(new ServiceFieldAvailability(ServiceField.DependsOn, "Denied", "ServiceController"));
        }
    }

    private static void QueryStatus(string name, ref ServiceStatus status, ref int? pid, ref ServiceControls accepted, ref int? exit, ref int? specific, ref int? checkpoint, ref TimeSpan? wait, List<ServiceFieldAvailability> availability)
    {
        var service = Open(name, ServiceNative.ServiceQueryStatus);
        if (service == 0) return;
        try
        {
            var size = Marshal.SizeOf<ServiceNative.ServiceStatusProcess>();
            var buffer = Marshal.AllocHGlobal(size);
            try
            {
                if (!ServiceNative.QueryServiceStatusEx(service, ServiceNative.StatusProcessInfo, buffer, size, out _))
                {
                    availability.Add(new ServiceFieldAvailability(ServiceField.Pid, "Denied", "QueryServiceStatusEx"));
                    return;
                }
                var row = Marshal.PtrToStructure<ServiceNative.ServiceStatusProcess>(buffer);
                status = (ServiceStatus)row.CurrentState;
                accepted = (ServiceControls)row.ControlsAccepted;
                exit = (int)row.Win32ExitCode;
                specific = (int)row.ServiceSpecificExitCode;
                checkpoint = (int)row.CheckPoint;
                wait = TimeSpan.FromMilliseconds(row.WaitHint);
                pid = row.ProcessId == 0 ? null : (int)row.ProcessId;
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
        finally { ServiceNative.CloseServiceHandle(service); }
    }

    private static bool? QueryDelayed(nint service)
    {
        var buffer = Marshal.AllocHGlobal(8);
        try
        {
            if (!ServiceNative.QueryServiceConfig2(service, ServiceNative.ServiceConfigDelayedAutoStart, buffer, 8, out _))
                return null;
            return Marshal.PtrToStructure<ServiceNative.DelayedAutoStartInfo>(buffer).DelayedAutostart;
        }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static bool QueryConfig(nint service, int size, out int needed)
        => ServiceNative.QueryServiceConfig(service, nint.Zero, 0, out needed);

    internal static nint Open(string name, uint access)
    {
        var scm = ServiceNative.OpenSCManager(ServiceMachine.Name, null, ServiceNative.ScManagerConnect);
        if (scm == 0) return 0;
        try { return ServiceNative.OpenService(scm, name, access); }
        finally { ServiceNative.CloseServiceHandle(scm); }
    }

    private static ServiceController? TryOpen(string name)
    {
        try { return new ServiceController(name, ServiceMachine.ControllerName); }
        catch { return null; }
    }

    private static string SafeDisplay(ServiceController controller)
    {
        try { return controller.DisplayName; }
        catch { return controller.ServiceName; }
    }

    private static string? NormalizeAccount(string? account)
    {
        if (string.IsNullOrWhiteSpace(account))
            return account;
        if (account.Equals("LocalSystem", StringComparison.OrdinalIgnoreCase)
            || account.Equals(@".\\LocalSystem", StringComparison.OrdinalIgnoreCase)
            || account.Equals(@"NT AUTHORITY\\SYSTEM", StringComparison.OrdinalIgnoreCase)
            || account.Equals("SYSTEM", StringComparison.OrdinalIgnoreCase))
            return "LocalSystem";
        return account;
    }

    private static ServiceStatus MapStatus(System.ServiceProcess.ServiceControllerStatus status)
        => status switch
        {
            System.ServiceProcess.ServiceControllerStatus.Stopped => ServiceStatus.Stopped,
            System.ServiceProcess.ServiceControllerStatus.StartPending => ServiceStatus.StartPending,
            System.ServiceProcess.ServiceControllerStatus.StopPending => ServiceStatus.StopPending,
            System.ServiceProcess.ServiceControllerStatus.Running => ServiceStatus.Running,
            System.ServiceProcess.ServiceControllerStatus.ContinuePending => ServiceStatus.ContinuePending,
            System.ServiceProcess.ServiceControllerStatus.PausePending => ServiceStatus.PausePending,
            System.ServiceProcess.ServiceControllerStatus.Paused => ServiceStatus.Paused,
            _ => ServiceStatus.Unknown
        };

    private static ServiceKind ParseKind(int type)
    {
        if ((type & (int)ServiceTypeFlags.KernelDriver) != 0 || (type & (int)ServiceTypeFlags.FileSystemDriver) != 0 || (type & (int)ServiceTypeFlags.RecognizerDriver) != 0)
            return ServiceKind.Driver;
        return ServiceKind.Win32;
    }

    private static bool LooksLikeService(int type)
        => (type & ((int)ServiceTypeFlags.KernelDriver | (int)ServiceTypeFlags.FileSystemDriver | (int)ServiceTypeFlags.Win32OwnProcess | (int)ServiceTypeFlags.Win32ShareProcess | (int)ServiceTypeFlags.UserService | (int)ServiceTypeFlags.PkgService)) != 0;

    private static bool MatchesKind(ServiceKind actual, ServiceKind filter)
        => filter == ServiceKind.All || actual == filter;

    private static IReadOnlyList<string> ParseMulti(string? raw)
        => string.IsNullOrWhiteSpace(raw) ? [] : raw.Split('\0', StringSplitOptions.RemoveEmptyEntries);

    private sealed record RawRow(string Name, string DisplayName, ServiceKind Kind, ServiceTypeFlags ServiceType, bool Hidden, ServiceController? Controller);
}
