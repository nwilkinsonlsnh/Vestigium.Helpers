using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Processes;

internal sealed class SystemKqlRow : IKqlRow
{
    private readonly SystemCounters _row;

    public SystemKqlRow(SystemCounters row) => _row = row;

    public KqlValue Get(string canonical)
    {
        return canonical switch
        {
            "SYS.ProcessCount" => Maybe(_row.ProcessCount),
            "SYS.ThreadCount" => Maybe(_row.ThreadCount),
            "SYS.HandleCount" => Maybe(_row.HandleCount),
            "CPU.Usage" => Maybe(_row.CpuPercent),
            "CPU.ContextSwitchDelta" => Maybe(_row.ContextSwitchDelta),
            "CPU.InterruptDelta" => Maybe(_row.InterruptDelta),
            "CPU.DpcDelta" => Maybe(_row.DpcDelta),
            "CPU.Cores" => Maybe(_row.Cores),
            "CPU.Sockets" => Maybe(_row.Sockets),
            "CPU.LogicalProcessors" => Maybe(_row.LogicalProcessors),
            "MEM.PhysicalTotal" => Maybe(_row.PhysicalTotal),
            "MEM.PhysicalAvailable" => Maybe(_row.PhysicalAvailable),
            "MEM.PhysicalPercent" => Maybe(_row.PhysicalMemoryPercent),
            "MEM.CommitCurrent" => Maybe(_row.CommitCurrent),
            "MEM.CommitLimit" => Maybe(_row.CommitLimit),
            "MEM.CommitPeak" => Maybe(_row.CommitPeak),
            "MEM.CommitPercent" => Maybe(_row.SystemCommitPercent),
            "MEM.CommitChange" => Maybe(_row.CommitChange),
            "MEM.CacheWS" => Maybe(_row.CacheWorkingSet),
            "MEM.KernelWS" => Maybe(_row.KernelWorkingSet),
            "MEM.DriverWS" => Maybe(_row.DriverWorkingSet),
            "MEM.Paged" => Maybe(_row.PagedWorkingSet),
            "MEM.Nonpaged" => Maybe(_row.Nonpaged),
            "MEM.PagedLimit" => Maybe(_row.PagedLimit),
            "MEM.NonpagedLimit" => Maybe(_row.NonpagedLimit),
            "MEM.Zeroed" => Maybe(_row.Zeroed),
            "MEM.Free" => Maybe(_row.Free),
            "MEM.Modified" => Maybe(_row.Modified),
            "MEM.Standby" => Maybe(_row.Standby),
            "MEM.PageFaultDelta" => Maybe(_row.PageFaultDelta),
            "IO.ReadDelta" => Maybe(_row.ReadDelta),
            "IO.WriteDelta" => Maybe(_row.WriteDelta),
            "IO.OtherDelta" => Maybe(_row.OtherDelta),
            "IO.ReadBytesDelta" => Maybe(_row.ReadBytesDelta),
            "IO.WriteBytesDelta" => Maybe(_row.WriteBytesDelta),
            "IO.OtherBytesDelta" => Maybe(_row.OtherBytesDelta),
            "IO.BytesPerSec" => Maybe(_row.IoThroughputBytesPerSec),
            "GPU.Usage" => Maybe(_row.GpuUsagePercent),
            "GPU.DedicatedMemory" => Maybe(_row.GpuDedicatedBytes),
            "GPU.SystemMemory" => Maybe(_row.GpuSystemBytes),
            "DISK.ReadDelta" => Maybe(_row.PageReadDelta),
            "DISK.WriteDelta" => Maybe(_row.PagingFileWriteDelta),
            "DISK.PagingFileWriteDelta" => Maybe(_row.PagingFileWriteDelta),
            "DISK.PageReadDelta" => Maybe(_row.PageReadDelta),
            "DISK.MappedFileWriteDelta" => Maybe(_row.MappedFileWriteDelta),
            _ => KqlValue.Unknown
        };
    }

    private static KqlValue Maybe<T>(T? value) where T : struct
        => value is { } present ? KqlValue.From(present) : KqlValue.Unknown;
}
