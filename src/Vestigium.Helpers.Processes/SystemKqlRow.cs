using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Processes;

internal sealed class SystemKqlRow : IKqlRow
{
    private static readonly Dictionary<string, Func<SystemCounters, KqlValue>> Fields =
        new(StringComparer.Ordinal)
        {
            ["SYS.ProcessCount"] = r => Maybe(r.ProcessCount),
            ["SYS.ThreadCount"] = r => Maybe(r.ThreadCount),
            ["SYS.HandleCount"] = r => Maybe(r.HandleCount),
            ["CPU.Usage"] = r => Maybe(r.CpuPercent),
            ["CPU.ContextSwitchDelta"] = r => Maybe(r.ContextSwitchDelta),
            ["CPU.InterruptDelta"] = r => Maybe(r.InterruptDelta),
            ["CPU.DpcDelta"] = r => Maybe(r.DpcDelta),
            ["CPU.Cores"] = r => Maybe(r.Cores),
            ["CPU.Sockets"] = r => Maybe(r.Sockets),
            ["CPU.LogicalProcessors"] = r => Maybe(r.LogicalProcessors),
            ["MEM.PhysicalTotal"] = r => Maybe(r.PhysicalTotal),
            ["MEM.PhysicalAvailable"] = r => Maybe(r.PhysicalAvailable),
            ["MEM.PhysicalPercent"] = r => Maybe(r.PhysicalMemoryPercent),
            ["MEM.CommitCurrent"] = r => Maybe(r.CommitCurrent),
            ["MEM.CommitLimit"] = r => Maybe(r.CommitLimit),
            ["MEM.CommitPeak"] = r => Maybe(r.CommitPeak),
            ["MEM.CommitPercent"] = r => Maybe(r.SystemCommitPercent),
            ["MEM.CommitChange"] = r => Maybe(r.CommitChange),
            ["MEM.CacheWS"] = r => Maybe(r.CacheWorkingSet),
            ["MEM.KernelWS"] = r => Maybe(r.KernelWorkingSet),
            ["MEM.DriverWS"] = r => Maybe(r.DriverWorkingSet),
            ["MEM.Paged"] = r => Maybe(r.PagedWorkingSet),
            ["MEM.Nonpaged"] = r => Maybe(r.Nonpaged),
            ["MEM.PagedLimit"] = r => Maybe(r.PagedLimit),
            ["MEM.NonpagedLimit"] = r => Maybe(r.NonpagedLimit),
            ["MEM.Zeroed"] = r => Maybe(r.Zeroed),
            ["MEM.Free"] = r => Maybe(r.Free),
            ["MEM.Modified"] = r => Maybe(r.Modified),
            ["MEM.Standby"] = r => Maybe(r.Standby),
            ["MEM.PageFaultDelta"] = r => Maybe(r.PageFaultDelta),
            ["IO.ReadDelta"] = r => Maybe(r.ReadDelta),
            ["IO.WriteDelta"] = r => Maybe(r.WriteDelta),
            ["IO.OtherDelta"] = r => Maybe(r.OtherDelta),
            ["IO.ReadBytesDelta"] = r => Maybe(r.ReadBytesDelta),
            ["IO.WriteBytesDelta"] = r => Maybe(r.WriteBytesDelta),
            ["IO.OtherBytesDelta"] = r => Maybe(r.OtherBytesDelta),
            ["IO.BytesPerSec"] = r => Maybe(r.IoThroughputBytesPerSec),
            ["GPU.Usage"] = r => Maybe(r.GpuUsagePercent),
            ["GPU.DedicatedMemory"] = r => Maybe(r.GpuDedicatedBytes),
            ["GPU.SystemMemory"] = r => Maybe(r.GpuSystemBytes),
            ["DISK.ReadDelta"] = r => Maybe(r.PageReadDelta),
            ["DISK.WriteDelta"] = r => Maybe(r.PagingFileWriteDelta),
            ["DISK.PagingFileWriteDelta"] = r => Maybe(r.PagingFileWriteDelta),
            ["DISK.PageReadDelta"] = r => Maybe(r.PageReadDelta),
            ["DISK.MappedFileWriteDelta"] = r => Maybe(r.MappedFileWriteDelta)
        };

    private readonly SystemCounters _row;

    public SystemKqlRow(SystemCounters row) => _row = row;

    public KqlValue Get(string canonical)
        => Fields.TryGetValue(canonical, out var read) ? read(_row) : KqlValue.Unknown;

    private static KqlValue Maybe<T>(T? value) where T : struct
        => value is { } present ? KqlValue.From(present) : KqlValue.Unknown;
}
