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
        var adapter = new[] { KqlPack.Adapter };
        var processSystem = new[] { KqlPack.Process, KqlPack.System };
        var processAdapterSystem = new[] { KqlPack.Process, KqlPack.Adapter, KqlPack.System };
        var systemAdapter = new[] { KqlPack.System, KqlPack.Adapter };

        return
        [
            F("PROC.Pid", KqlType.Integer, KqlGroups.Proc, process, "PID", "Pid"),
            F("PROC.ParentPid", KqlType.Integer, KqlGroups.Proc, process, "PPID"),
            F("PROC.Name", KqlType.String, KqlGroups.Proc, process, "Name"),
            F("PROC.ImagePath", KqlType.String, KqlGroups.Proc, process, "Image", "Path"),
            F("PROC.CommandLine", KqlType.String, KqlGroups.Proc, process, "Cmd"),
            F("PROC.Session", KqlType.Integer, KqlGroups.Proc, process, "SessionId"),
            F("PROC.Threads", KqlType.Integer, KqlGroups.Proc, process),
            F("PROC.Handles", KqlType.Integer, KqlGroups.Proc, process),
            F("PROC.StartTime", KqlType.DateTime, KqlGroups.Proc, process),
            F("PROC.Company", KqlType.String, KqlGroups.Proc, process),
            F("PROC.ImageType", KqlType.String, KqlGroups.Proc, process),
            F("PROC.Integrity", KqlType.String, KqlGroups.Proc, process),
            F("PROC.WindowTitle", KqlType.String, KqlGroups.Proc, process),

            F("CPU.Usage", KqlType.Number, KqlGroups.Cpu, processSystem, "CPU", "CPU.Percent"),
            F("CPU.Time", KqlType.TimeSpan, KqlGroups.Cpu, process, "CPU.Total"),
            F("CPU.UserTime", KqlType.TimeSpan, KqlGroups.Cpu, process),
            F("CPU.KernelTime", KqlType.TimeSpan, KqlGroups.Cpu, process),
            F("CPU.TimeDelta", KqlType.TimeSpan, KqlGroups.Cpu, process),
            F("CPU.Priority", KqlType.Integer, KqlGroups.Cpu, process),
            F("CPU.ContextSwitchDelta", KqlType.Integer, KqlGroups.Cpu, system),
            F("CPU.InterruptDelta", KqlType.Integer, KqlGroups.Cpu, system),
            F("CPU.DpcDelta", KqlType.Integer, KqlGroups.Cpu, system),
            F("CPU.Cores", KqlType.Integer, KqlGroups.Cpu, system),
            F("CPU.Sockets", KqlType.Integer, KqlGroups.Cpu, system),
            F("CPU.LogicalProcessors", KqlType.Integer, KqlGroups.Cpu, system),

            F("MEM.PrivateBytes", KqlType.Integer, KqlGroups.Mem, process, "RAM.PrivateBytes", "CPU.PrivateBytes", "PrivateBytes"),
            F("MEM.WorkingSet", KqlType.Integer, KqlGroups.Mem, process, "RAM.WorkingSet", "WorkingSet"),
            F("MEM.Commit", KqlType.Integer, KqlGroups.Mem, process, "RAM.Commit", "Commit"),
            F("MEM.VirtualBytes", KqlType.Integer, KqlGroups.Mem, process),
            F("MEM.WorkingSetPeak", KqlType.Integer, KqlGroups.Mem, process),
            F("MEM.PageFaults", KqlType.Integer, KqlGroups.Mem, process),
            F("MEM.PageFaultDelta", KqlType.Integer, KqlGroups.Mem, processSystem),
            F("MEM.PrivateBytesDelta", KqlType.Integer, KqlGroups.Mem, process),
            F("MEM.WorkingSetDelta", KqlType.Integer, KqlGroups.Mem, process),
            F("MEM.PhysicalTotal", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.PhysicalAvailable", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.PhysicalPercent", KqlType.Number, KqlGroups.Mem, system),
            F("MEM.CommitCurrent", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.CommitLimit", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.CommitPeak", KqlType.Integer, KqlGroups.Mem, system),
            F("MEM.CommitPercent", KqlType.Number, KqlGroups.Mem, system),
            F("MEM.CommitChange", KqlType.Integer, KqlGroups.Mem, system),
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

            F("GPU.Usage", KqlType.Number, KqlGroups.Gpu, processAdapterSystem, "GPU.UsagePercent"),
            F("GPU.DedicatedMemory", KqlType.Integer, KqlGroups.Gpu, processAdapterSystem, "GPU.DedicatedBytes"),
            F("GPU.SystemMemory", KqlType.Integer, KqlGroups.Gpu, processAdapterSystem, "GPU.SharedBytes", "GPU.SystemBytes"),
            F("GPU.CommittedMemory", KqlType.Integer, KqlGroups.Gpu, processAdapterSystem, "GPU.Commit"),
            F("GPU.Adapter", KqlType.String, KqlGroups.Gpu, processAdapterSystem),
            F("GPU.Engine", KqlType.String, KqlGroups.Gpu, processAdapterSystem),

            F("IO.Reads", KqlType.Integer, KqlGroups.Io, processSystem, "CPU.IO.Reads"),
            F("IO.ReadBytes", KqlType.Integer, KqlGroups.Io, processSystem, "CPU.IO.ReadBytes"),
            F("IO.Writes", KqlType.Integer, KqlGroups.Io, processSystem, "CPU.IO.Writes"),
            F("IO.WriteBytes", KqlType.Integer, KqlGroups.Io, processSystem, "CPU.IO.WriteBytes"),
            F("IO.ReadDelta", KqlType.Integer, KqlGroups.Io, processSystem),
            F("IO.WriteDelta", KqlType.Integer, KqlGroups.Io, processSystem),
            F("IO.Other", KqlType.Integer, KqlGroups.Io, processSystem),
            F("IO.OtherBytes", KqlType.Integer, KqlGroups.Io, processSystem),
            F("IO.OtherDelta", KqlType.Integer, KqlGroups.Io, processSystem),
            F("IO.OtherBytesDelta", KqlType.Integer, KqlGroups.Io, processSystem),
            F("IO.ReadBytesDelta", KqlType.Integer, KqlGroups.Io, processSystem),
            F("IO.WriteBytesDelta", KqlType.Integer, KqlGroups.Io, processSystem),
            F("IO.BytesPerSec", KqlType.Integer, KqlGroups.Io, processSystem),

            F("DISK.ReadDelta", KqlType.Integer, KqlGroups.Disk, system),
            F("DISK.WriteDelta", KqlType.Integer, KqlGroups.Disk, system),
            F("DISK.PagingFileWriteDelta", KqlType.Integer, KqlGroups.Disk, system),
            F("DISK.PageReadDelta", KqlType.Integer, KqlGroups.Disk, system),
            F("DISK.MappedFileWriteDelta", KqlType.Integer, KqlGroups.Disk, system),

            F("NET.BytesSentDelta", KqlType.Integer, KqlGroups.Net, systemAdapter),
            F("NET.BytesRecvDelta", KqlType.Integer, KqlGroups.Net, systemAdapter),
            F("NET.Connections", KqlType.Integer, KqlGroups.Net, system),

            F("SVC.Name", KqlType.String, KqlGroups.Svc, service, "Name"),
            F("SVC.DisplayName", KqlType.String, KqlGroups.Svc, service),
            F("SVC.Status", KqlType.String, KqlGroups.Svc, service, "State"),
            F("SVC.StartType", KqlType.String, KqlGroups.Svc, service),
            F("SVC.Pid", KqlType.Integer, KqlGroups.Svc, service, "PID"),

            F("THR.Tid", KqlType.Integer, KqlGroups.Thr, thread, "TID"),
            F("THR.State", KqlType.String, KqlGroups.Thr, thread),
            F("THR.StartAddress", KqlType.String, KqlGroups.Thr, thread),
            F("THR.Cpu", KqlType.Number, KqlGroups.Cpu, thread),

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
        params string[] aliases)
        => new(canonical, type, group, packs, aliases);
}
