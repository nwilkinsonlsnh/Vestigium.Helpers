using System.ServiceProcess;
using Microsoft.Win32;
using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Services;

internal static partial class ServiceSnapshotter
{
    public static IReadOnlyList<ServiceInfo> Capture(
        ServiceDetailLevel level,
        ServiceKind kind,
        ServiceListScope scope,
        bool joinProcess)
    {
        var visible = ReadVisible();
        var visibleNames = new HashSet<string>(visible.Keys, StringComparer.OrdinalIgnoreCase);
        IReadOnlyList<RawRow> hidden = scope == ServiceListScope.Visible
            ? Array.Empty<RawRow>()
            : ReadHidden(visibleNames);

        IEnumerable<RawRow> rows = scope switch
        {
            ServiceListScope.Visible => visible.Values,
            ServiceListScope.Hidden => hidden,
            _ => visible.Values.Concat(hidden)
        };

        var result = new List<ServiceInfo>();
        foreach (var raw in rows)
        {
            if (!MatchesKind(raw.Kind, kind))
                continue;
            result.Add(Materialize(raw, level, joinProcess && ServiceMachine.IsLocal));
        }

        return result.OrderBy(r => r.Name, StringComparer.OrdinalIgnoreCase).ToArray();
    }

    public static ServiceInfo? CaptureName(string name, ServiceDetailLevel level, bool joinProcess)
    {
        ServiceController? controller = null;
        try
        {
            controller = new ServiceController(name, ServiceMachine.ControllerName);
            _ = controller.Status;
            var raw = new RawRow(
                controller.ServiceName,
                SafeDisplay(controller),
                ParseKind((int)controller.ServiceType),
                (ServiceTypeFlags)(int)controller.ServiceType,
                Hidden: false,
                Controller: controller);
            return Materialize(raw, level, joinProcess && ServiceMachine.IsLocal);
        }
        catch (InvalidOperationException)
        {
            controller?.Dispose();
            return CaptureHiddenName(name, level, joinProcess);
        }
        catch
        {
            controller?.Dispose();
            return CaptureHiddenName(name, level, joinProcess);
        }
    }

    private static ServiceInfo? CaptureHiddenName(string name, ServiceDetailLevel level, bool joinProcess)
    {
        try
        {
            using var key = OpenServiceKey(name);
            if (key is null)
                return null;
            var type = Convert.ToInt32(key.GetValue("Type", 0) ?? 0);
            if (type == 0 || !LooksLikeService(type))
                return null;
            var raw = new RawRow(
                name,
                key.GetValue("DisplayName") as string ?? name,
                ParseKind(type),
                (ServiceTypeFlags)type,
                Hidden: true,
                Controller: TryOpen(name));
            return Materialize(raw, level, joinProcess && ServiceMachine.IsLocal);
        }
        catch
        {
            return null;
        }
    }

    private static Dictionary<string, RawRow> ReadVisible()
    {
        var map = new Dictionary<string, RawRow>(StringComparer.OrdinalIgnoreCase);
        var machine = ServiceMachine.ControllerName;
        TryAddControllers(map, () => ServiceController.GetServices(machine));
        TryAddControllers(map, () => ServiceController.GetDevices(machine));
        return map;
    }

    private static void TryAddControllers(Dictionary<string, RawRow> map, Func<ServiceController[]> source)
    {
        try
        {
            foreach (var controller in source())
                AddController(map, controller, hidden: false);
        }
        catch
        {
        }
    }

    private static List<RawRow> ReadHidden(HashSet<string> visibleNames)
    {
        var hidden = new List<RawRow>();
        try
        {
            using var key = OpenServicesRoot();
            if (key is null)
                return hidden;

            foreach (var name in key.GetSubKeyNames())
            {
                if (visibleNames.Contains(name))
                    continue;
                using var sub = key.OpenSubKey(name);
                if (sub is null)
                    continue;
                var type = Convert.ToInt32(sub.GetValue("Type", 0) ?? 0);
                if (type == 0 || !LooksLikeService(type))
                    continue;
                hidden.Add(new RawRow(
                    name,
                    sub.GetValue("DisplayName") as string ?? name,
                    ParseKind(type),
                    (ServiceTypeFlags)type,
                    Hidden: true,
                    Controller: TryOpen(name)));
            }
        }
        catch
        {
        }

        return hidden;
    }

    private static RegistryKey? OpenServicesRoot()
    {
        try
        {
            if (ServiceMachine.IsLocal)
                return Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
            return RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, ServiceMachine.Name!)
                .OpenSubKey(@"SYSTEM\CurrentControlSet\Services");
        }
        catch
        {
            return null;
        }
    }

    private static RegistryKey? OpenServiceKey(string name)
    {
        try
        {
            if (ServiceMachine.IsLocal)
                return Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + name);
            return RegistryKey.OpenRemoteBaseKey(RegistryHive.LocalMachine, ServiceMachine.Name!)
                .OpenSubKey(@"SYSTEM\CurrentControlSet\Services\" + name);
        }
        catch
        {
            return null;
        }
    }

    private static void AddController(Dictionary<string, RawRow> map, ServiceController controller, bool hidden)
    {
        try
        {
            var name = controller.ServiceName;
            map[name] = new RawRow(
                name,
                SafeDisplay(controller),
                ParseKind((int)controller.ServiceType),
                (ServiceTypeFlags)(int)controller.ServiceType,
                Hidden: hidden,
                Controller: controller);
        }
        catch
        {
            controller.Dispose();
        }
    }

    private static ServiceInfo Materialize(RawRow raw, ServiceDetailLevel level, bool joinProcess)
    {
        var availability = new List<ServiceFieldAvailability>();
        var status = ServiceStatus.Unknown;
        int? pid = null;
        ServiceControls accepted = ServiceControls.None;
        bool canPause = false;
        int? exit = null;
        int? specific = null;
        int? checkpoint = null;
        TimeSpan? wait = null;

        if (raw.Controller is not null)
        {
            try
            {
                raw.Controller.Refresh();
                status = MapStatus(raw.Controller.Status);
                canPause = raw.Controller.CanPauseAndContinue;
            }
            catch
            {
                availability.Add(new ServiceFieldAvailability(ServiceField.Status, "Denied", "ServiceController"));
            }
        }

        QueryStatus(raw.Name, ref status, ref pid, ref accepted, ref exit, ref specific, ref checkpoint, ref wait, availability);

        var info = new ServiceInfo
        {
            Name = raw.Name,
            Machine = ServiceMachine.Name,
            DisplayName = raw.DisplayName,
            Kind = raw.Kind,
            ServiceType = raw.ServiceType,
            SharedProcess = raw.ServiceType.HasFlag(ServiceTypeFlags.Win32ShareProcess),
            IsHidden = raw.Hidden,
            Status = status,
            Pid = pid,
            Win32ExitCode = exit,
            ServiceSpecificExitCode = specific,
            Checkpoint = checkpoint,
            WaitHint = wait,
            ControlsAccepted = accepted,
            CanPauseAndContinue = canPause
        };

        if (level != ServiceDetailLevel.Identity)
        {
            FillConfig(raw.Name, info, availability);
            FillDepends(raw.Controller, info, availability);
        }

        if (level == ServiceDetailLevel.Full)
        {
            FillDescription(raw.Name, info, availability);
            FillFailure(raw.Name, info, availability);
        }

        if (joinProcess && pid is > 0 && ServiceMachine.IsLocal)
        {
            try { info.Process = ProcessHelper.Get(pid.Value, ProcessDetailLevel.Slim); }
            catch { }
        }

        info.Availability = availability;
        raw.Controller?.Dispose();
        return info;
    }
}
