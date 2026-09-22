namespace Vestigium.Helpers.Kql;

internal static class KqlCatalog
{
    internal static readonly IReadOnlyList<KqlField> All = Build();

    internal static IReadOnlyList<KqlField> For(IReadOnlyList<KqlPack> packs, KqlGroups groups)
    {
        if (packs.Count == 0)
            packs = [KqlPack.Process];

        var enabledGroups = groups == KqlGroups.None
            ? DefaultGroups(packs)
            : groups;

        return All
            .Where(field => field.Packs.Any(packs.Contains) && enabledGroups.HasFlag(field.Group))
            .ToArray();
    }

    internal static KqlGroups DefaultGroups(IReadOnlyList<KqlPack> packs)
    {
        var groups = KqlGroups.None;
        foreach (var pack in packs)
        {
            groups |= pack switch
            {
                KqlPack.Process => KqlGroups.Proc | KqlGroups.Cpu | KqlGroups.Mem | KqlGroups.Io | KqlGroups.Gpu,
                KqlPack.Service => KqlGroups.Svc,
                KqlPack.Thread => KqlGroups.Thr | KqlGroups.Cpu,
                KqlPack.System => KqlGroups.Sys | KqlGroups.Cpu | KqlGroups.Mem | KqlGroups.Gpu | KqlGroups.Disk | KqlGroups.Io | KqlGroups.Net,
                KqlPack.Adapter => KqlGroups.Gpu | KqlGroups.Net,
                _ => KqlGroups.None
            };
        }

        return groups;
    }

    private static IReadOnlyList<KqlField> Build()
    {
        var process = new[] { KqlPack.Process };
        var service = new[] { KqlPack.Service };
        var thread = new[] { KqlPack.Thread };
        var system = new[] { KqlPack.System };
        var processSystem = new[] { KqlPack.Process, KqlPack.System };
        var processAdapterSystem = new[] { KqlPack.Process, KqlPack.Adapter, KqlPack.System };
        var systemAdapter = new[] { KqlPack.System, KqlPack.Adapter };

        return
        [
            F("PROC.Pid", KqlType.Integer, KqlGroups.Proc, process, false, "PID", "Pid"),
            F("PROC.ParentPid", KqlType.Integer, KqlGroups.Proc, process, false, "PPID"),
            F("PROC.Name", KqlType.String, KqlGroups.Proc, process, false, "Name"),
            F("PROC.ImagePath", KqlType.String, KqlGroups.Proc, process, false, "Image", "Path"),
            F("PROC.CommandLine", KqlType.String, KqlGroups.Proc, process, false, "Cmd"),
            F("PROC.Session", KqlType.Integer, KqlGroups.Proc, process, false, "SessionId"),
            F("PROC.Threads", KqlType.Integer, KqlGroups.Proc, process),
            F("PROC.Handles", KqlType.Integer, KqlGroups.Proc, process),
            F("PROC.StartTime", KqlType.DateTime, KqlGroups.Proc, process),
            F("PROC.Company", KqlType.String, KqlGroups.Proc, process),
            F("PROC.ImageType", KqlType.String, KqlGroups.Proc, process),
            F("PROC.Integrity", KqlType.String, KqlGroups.Proc, process),
            F("PROC.WindowTitle", KqlType.String, KqlGroups.Proc, process),
            F("PROC.Description", KqlType.String, KqlGroups.Proc, process, false, "Description"),
            F("PROC.Version", KqlType.String, KqlGroups.Proc, process, false, "Version"),
            F("PROC.Signer", KqlType.String, KqlGroups.Proc, process, false, "Signer"),
            F("PROC.SignerTrust", KqlType.String, KqlGroups.Proc, process),
            F("PROC.Package", KqlType.String, KqlGroups.Proc, process, false, "Package"),
            F("PROC.Autostart", KqlType.String, KqlGroups.Proc, process),
            F("PROC.Comment", KqlType.String, KqlGroups.Proc, process, false, "Comment"),
            F("PROC.WindowStatus", KqlType.String, KqlGroups.Proc, process),
            F("PROC.Dep", KqlType.String, KqlGroups.Proc, process),
            F("PROC.Aslr", KqlType.Boolean, KqlGroups.Proc, process),
            F("PROC.Cfg", KqlType.String, KqlGroups.Proc, process),
            F("PROC.StackProtection", KqlType.String, KqlGroups.Proc, process),
            F("PROC.Protection", KqlType.String, KqlGroups.Proc, process),
            F("PROC.Dpi", KqlType.String, KqlGroups.Proc, process),
            F("PROC.UiAccess", KqlType.Boolean, KqlGroups.Proc, process),
            F("PROC.Virtualized", KqlType.Boolean, KqlGroups.Proc, process),
            F("PROC.EnterpriseContext", KqlType.String, KqlGroups.Proc, process),

            F("CPU.Usage", KqlType.Number, KqlGroups.Cpu, processSystem, true, "CPU", "CPU.Percent"),
            F("CPU.Time", KqlType.TimeSpan, KqlGroups.Cpu, process, false, "CPU.Total"),
            F("CPU.UserTime", KqlType.TimeSpan, KqlGroups.Cpu, process),
            F("CPU.KernelTime", KqlType.TimeSpan, KqlGroups.Cpu, process),
            F("CPU.TimeDelta", KqlType.TimeSpan, KqlGroups.Cpu, process, true),
            F("CPU.Priority", KqlType.Integer, KqlGroups.Cpu, process),
            F("CPU.ContextSwitchDelta", KqlType.Integer, KqlGroups.Cpu, system, true),
            F("CPU.InterruptDelta", KqlType.Integer, KqlGroups.Cpu, system, true),
            F("CPU.DpcDelta", KqlType.Integer, KqlGroups.Cpu, system, true),
            F("CPU.Cores", KqlType.Integer, KqlGroups.Cpu, system),
            F("CPU.Sockets", KqlType.Integer, KqlGroups.Cpu, system),
            F("CPU.LogicalProcessors", KqlType.Integer, KqlGroups.Cpu, system),

            F("MEM.PrivateBytes", KqlType.Integer, KqlGroups.Mem, process, false, "RAM.PrivateBytes", "CPU.PrivateBytes", "PrivateBytes"),
            F("MEM.WorkingSet", KqlType.Integer, KqlGroups.Mem, process, false, "RAM.WorkingSet", "WorkingSet"),
            F("MEM.Commit", KqlType.Integer, KqlGroups.Mem, process, false, "RAM.Commit", "Commit"),
            F("MEM.VirtualBytes", KqlType.Integer, KqlGroups.Mem, process),
            F("MEM.WorkingSetPeak", KqlType.Integer, KqlGroups.Mem, process),
            F("MEM.PageFaults", KqlType.Integer, KqlGroups.Mem, process),
            F("MEM.PageFaultDelta", KqlType.Integer, KqlGroups.Mem, processSystem, true),
            F("MEM.PrivateBytesDelta", KqlType.Integer, KqlGroups.Mem, process, true),
            F("MEM.WorkingSetDelta", KqlType.Integer, KqlGroups.Mem, process, true),
            F("MEM.PhysicalTotal", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.PhysicalAvailable", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.PhysicalPercent", KqlType.Number, KqlGroups.Mem, system),
            F("MEM.CommitCurrent", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.CommitLimit", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.CommitPeak", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.CommitPercent", KqlType.Number, KqlGroups.Mem, system),
            F("MEM.CommitChange", KqlType.Integer, KqlGroups.Mem, system, true),
            F("MEM.CacheWS", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.KernelWS", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.DriverWS", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.Paged", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.Nonpaged", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.PagedLimit", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.NonpagedLimit", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.Zeroed", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.Free", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.Modified", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.Standby", KqlType.Integer, KqlGroups.Mem, system),

            F("GPU.Usage", KqlType.Number, KqlGroups.Gpu, processAdapterSystem, true, "GPU.UsagePercent"),
            F("GPU.DedicatedMemory", KqlType.Integer, KqlGroups.Gpu, processAdapterSystem, false, "GPU.DedicatedBytes"),
            F("GPU.SystemMemory", KqlType.Integer, KqlGroups.Gpu, processAdapterSystem, false, "GPU.SharedBytes", "GPU.SystemBytes"),
            F("GPU.CommittedMemory", KqlType.Integer, KqlGroups.Gpu, processAdapterSystem, false, "GPU.Commit"),
            F("GPU.Adapter", KqlType.String, KqlGroups.Gpu, processAdapterSystem),
            F("GPU.Engine", KqlType.String, KqlGroups.Gpu, processAdapterSystem),

            F("IO.Reads", KqlType.Integer, KqlGroups.Io, processSystem, false, "CPU.IO.Reads"),
            F("IO.ReadBytes", KqlType.Integer, KqlGroups.Io, processSystem, false, "CPU.IO.ReadBytes"),
            F("IO.Writes", KqlType.Integer, KqlGroups.Io, processSystem, false, "CPU.IO.Writes"),
            F("IO.WriteBytes", KqlType.Integer, KqlGroups.Io, processSystem, false, "CPU.IO.WriteBytes"),
            F("IO.ReadDelta", KqlType.Integer, KqlGroups.Io, processSystem, true),
            F("IO.WriteDelta", KqlType.Integer, KqlGroups.Io, processSystem, true),
            F("IO.Other", KqlType.Integer, KqlGroups.Io, processSystem),
            F("IO.OtherBytes", KqlType.Integer, KqlGroups.Io, processSystem),
            F("IO.OtherDelta", KqlType.Integer, KqlGroups.Io, processSystem, true),
            F("IO.OtherBytesDelta", KqlType.Integer, KqlGroups.Io, processSystem, true),
            F("IO.ReadBytesDelta", KqlType.Integer, KqlGroups.Io, processSystem, true),
            F("IO.WriteBytesDelta", KqlType.Integer, KqlGroups.Io, processSystem, true),
            F("IO.BytesPerSec", KqlType.Integer, KqlGroups.Io, processSystem, true),

            F("DISK.ReadDelta", KqlType.Integer, KqlGroups.Disk, system, true),
            F("DISK.WriteDelta", KqlType.Integer, KqlGroups.Disk, system, true),
            F("DISK.PagingFileWriteDelta", KqlType.Integer, KqlGroups.Disk, system, true),
            F("DISK.PageReadDelta", KqlType.Integer, KqlGroups.Disk, system, true),
            F("DISK.MappedFileWriteDelta", KqlType.Integer, KqlGroups.Disk, system, true),

            F("NET.BytesSentDelta", KqlType.Integer, KqlGroups.Net, systemAdapter, true),
            F("NET.BytesRecvDelta", KqlType.Integer, KqlGroups.Net, systemAdapter, true),
            F("NET.Connections", KqlType.Integer, KqlGroups.Net, system),

            F("SVC.Name", KqlType.String, KqlGroups.Svc, service, false, "Name"),
            F("SVC.DisplayName", KqlType.String, KqlGroups.Svc, service),
            F("SVC.Status", KqlType.String, KqlGroups.Svc, service, false, "State"),
            F("SVC.StartType", KqlType.String, KqlGroups.Svc, service),
            F("SVC.Pid", KqlType.Integer, KqlGroups.Svc, service, false, "PID"),

            F("THR.Tid", KqlType.Integer, KqlGroups.Thr, thread, false, "TID"),
            F("THR.State", KqlType.String, KqlGroups.Thr, thread),
            F("THR.StartAddress", KqlType.String, KqlGroups.Thr, thread),
            F("THR.Cpu", KqlType.Number, KqlGroups.Cpu, thread, true),

            F("SYS.ProcessCount", KqlType.Integer, KqlGroups.Sys, system),
            F("SYS.ThreadCount", KqlType.Integer, KqlGroups.Sys, system),
            F("SYS.HandleCount", KqlType.Integer, KqlGroups.Sys, system)
        ];
    }

    private static KqlField F(
        string canonical,
        KqlType type,
        KqlGroups group,
        IReadOnlyList<KqlPack> packs,
        bool watchOnly = false,
        params string[] aliases)
        => new(canonical, type, group, packs, aliases, watchOnly);
}
