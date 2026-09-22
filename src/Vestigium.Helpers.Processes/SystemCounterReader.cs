using System.Runtime.InteropServices;

namespace Vestigium.Helpers.Processes;

internal static class SystemCounterReader
{
    private const int SystemPerformanceInformation = 2;
    private const int SystemInterruptInformation = 23;
    private const int SystemProcessorPerformanceInformation = 8;
    private const int SystemMemoryListInformation = 80;
    private const int RelationProcessorCore = 0;
    private const int RelationProcessorPackage = 3;
    private const int RelationAll = 0xFFFF;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool GetPerformanceInfo(out PerformanceInformation info, int size);

    [DllImport("ntdll.dll")]
    private static extern int NtQuerySystemInformation(int cls, nint buffer, int length, out int returned);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetLogicalProcessorInformationEx(int relationship, nint buffer, ref int length);

    [StructLayout(LayoutKind.Sequential)]
    private struct FileTime
    {
        public uint Low;
        public uint High;
        public ulong Ticks => ((ulong)High << 32) | Low;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct MemoryStatusEx
    {
        public uint Length;
        public uint MemoryLoad;
        public ulong TotalPhys;
        public ulong AvailPhys;
        public ulong TotalPageFile;
        public ulong AvailPageFile;
        public ulong TotalVirtual;
        public ulong AvailVirtual;
        public ulong AvailExtendedVirtual;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct PerformanceInformation
    {
        public int Size;
        public nint CommitTotal;
        public nint CommitLimit;
        public nint CommitPeak;
        public nint PhysicalTotal;
        public nint PhysicalAvailable;
        public nint SystemCache;
        public nint KernelTotal;
        public nint KernelPaged;
        public nint KernelNonpaged;
        public nint PageSize;
        public int HandleCount;
        public int ProcessCount;
        public int ThreadCount;
    }

    internal static SystemCounters Capture(SystemRawSnapshot? previous, TimeSpan? interval, out SystemRawSnapshot raw)
    {
        var times = ReadTimes();
        var perfIo = ReadPerformanceBlock();
        var interrupts = ReadInterruptTotals();
        var lists = ReadMemoryLists();
        var topo = ReadTopology();
        var gpu = ProcessGpuCatalog.System();

        long? physTotal = null, physAvail = null, commitCurrent = null, commitLimit = null, commitPeak = null;
        long? cacheWs = null, pagedWs = null, nonpaged = null, kernelWs = null;
        int? processes = null, threads = null, handles = null;
        long page = 4096;

        var mem = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (GlobalMemoryStatusEx(ref mem))
        {
            physTotal = checked((long)mem.TotalPhys);
            physAvail = checked((long)mem.AvailPhys);
            commitLimit = checked((long)mem.TotalPageFile);
            commitCurrent = checked((long)(mem.TotalPageFile - mem.AvailPageFile));
        }

        var perf = new PerformanceInformation { Size = Marshal.SizeOf<PerformanceInformation>() };
        if (GetPerformanceInfo(out perf, perf.Size))
        {
            page = perf.PageSize == 0 ? 4096L : perf.PageSize.ToInt64();
            processes = perf.ProcessCount;
            threads = perf.ThreadCount;
            handles = perf.HandleCount;
            commitPeak = perf.CommitPeak.ToInt64() * page;
            cacheWs = perf.SystemCache.ToInt64() * page;
            pagedWs = perf.KernelPaged.ToInt64() * page;
            nonpaged = perf.KernelNonpaged.ToInt64() * page;
            kernelWs = perf.KernelTotal.ToInt64() * page;
        }

        raw = new SystemRawSnapshot
        {
            Times = times,
            ReadOperations = perfIo.ReadOps,
            WriteOperations = perfIo.WriteOps,
            OtherOperations = perfIo.OtherOps,
            ReadBytes = perfIo.ReadBytes,
            WriteBytes = perfIo.WriteBytes,
            OtherBytes = perfIo.OtherBytes,
            PageFaults = perfIo.PageFaults,
            PageReads = perfIo.PageReads,
            PagingFileWrites = perfIo.DirtyWrites,
            MappedFileWrites = perfIo.MappedWrites,
            ContextSwitches = interrupts.ContextSwitches,
            Interrupts = interrupts.Interrupts,
            Dpcs = interrupts.Dpcs,
            CommitCurrent = commitCurrent ?? 0
        };

        double? cpu = null;
        if (previous?.Times is { } priorTimes && times is { } nowTimes && interval is not null)
        {
            var idle = Diff(nowTimes.Idle, priorTimes.Idle);
            var kernel = Diff(nowTimes.Kernel, priorTimes.Kernel);
            var user = Diff(nowTimes.User, priorTimes.User);
            var total = kernel + user;
            if (total > 0)
                cpu = 100.0 * (total - idle) / total;
        }

        long? Delta(long now, long then) => previous is null || interval is null ? null : now - then;

        var readBytesDelta = Delta(raw.ReadBytes, previous?.ReadBytes ?? 0);
        var writeBytesDelta = Delta(raw.WriteBytes, previous?.WriteBytes ?? 0);
        var otherBytesDelta = Delta(raw.OtherBytes, previous?.OtherBytes ?? 0);
        long? ioPerSec = null;
        if (interval is { } span && span.TotalSeconds > 0 && readBytesDelta is long r && writeBytesDelta is long w && otherBytesDelta is long o)
            ioPerSec = (long)((r + w + o) / span.TotalSeconds);

        double? commitPct = commitCurrent is long cur && commitLimit is long lim && lim > 0 ? (double)cur / lim : null;
        double? physPct = physTotal is long tot && physAvail is long av && tot > 0 ? (double)(tot - av) / tot : null;

        return new SystemCounters
        {
            Timestamp = DateTimeOffset.Now,
            Interval = interval,
            CpuPercent = cpu,
            SystemCommitPercent = commitPct,
            PhysicalMemoryPercent = physPct,
            IoThroughputBytesPerSec = ioPerSec,
            GpuUsagePercent = gpu.UsagePercent,
            GpuDedicatedBytes = gpu.DedicatedBytes,
            GpuSystemBytes = gpu.SystemBytes,
            GpuAdapters = gpu.Adapters,
            ReadOperations = raw.ReadOperations,
            WriteOperations = raw.WriteOperations,
            OtherOperations = raw.OtherOperations,
            ReadBytes = raw.ReadBytes,
            WriteBytes = raw.WriteBytes,
            OtherBytes = raw.OtherBytes,
            ReadDelta = Delta(raw.ReadOperations, previous?.ReadOperations ?? 0),
            WriteDelta = Delta(raw.WriteOperations, previous?.WriteOperations ?? 0),
            OtherDelta = Delta(raw.OtherOperations, previous?.OtherOperations ?? 0),
            ReadBytesDelta = readBytesDelta,
            WriteBytesDelta = writeBytesDelta,
            OtherBytesDelta = otherBytesDelta,
            CommitCurrent = commitCurrent,
            CommitLimit = commitLimit,
            CommitPeak = commitPeak,
            CommitChange = previous is null || interval is null || commitCurrent is null ? null : commitCurrent - previous.CommitCurrent,
            CommitCurrentToLimit = commitPct,
            CommitPeakToLimit = commitPeak is long peak && commitLimit is long lim2 && lim2 > 0 ? (double)peak / lim2 : null,
            PhysicalTotal = physTotal,
            PhysicalAvailable = physAvail,
            CacheWorkingSet = cacheWs,
            KernelWorkingSet = kernelWs,
            DriverWorkingSet = kernelWs is long k && cacheWs is long c ? Math.Max(0, k - c) : null,
            PagedWorkingSet = pagedWs,
            PagedVirtual = pagedWs,
            Nonpaged = nonpaged,
            PageFaultDelta = Delta(raw.PageFaults, previous?.PageFaults ?? 0),
            PageReadDelta = Delta(raw.PageReads, previous?.PageReads ?? 0),
            PagingFileWriteDelta = Delta(raw.PagingFileWrites, previous?.PagingFileWrites ?? 0),
            MappedFileWriteDelta = Delta(raw.MappedFileWrites, previous?.MappedFileWrites ?? 0),
            Zeroed = lists.Zeroed * page,
            Free = lists.Free * page,
            Modified = lists.Modified * page,
            ModifiedNoWrite = lists.ModifiedNoWrite * page,
            Standby = lists.Standby * page,
            Priority0 = lists.P0 * page,
            Priority1 = lists.P1 * page,
            Priority2 = lists.P2 * page,
            Priority3 = lists.P3 * page,
            Priority4 = lists.P4 * page,
            Priority5 = lists.P5 * page,
            Priority6 = lists.P6 * page,
            Priority7 = lists.P7 * page,
            PagedFileModified = lists.PagedFileModified * page,
            ProcessCount = processes,
            ThreadCount = threads,
            HandleCount = handles,
            ContextSwitchDelta = Delta(raw.ContextSwitches, previous?.ContextSwitches ?? 0),
            InterruptDelta = Delta(raw.Interrupts, previous?.Interrupts ?? 0),
            DpcDelta = Delta(raw.Dpcs, previous?.Dpcs ?? 0),
            Cores = topo.Cores,
            Sockets = topo.Sockets,
            LogicalProcessors = topo.Logical
        };
    }

    internal static SystemTimes? ReadTimes()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
            return null;
        return new SystemTimes(idle.Ticks, kernel.Ticks, user.Ticks);
    }

    private static ulong Diff(ulong now, ulong then) => now >= then ? now - then : 0;

    private static PerformanceBlock ReadPerformanceBlock()
    {
        var data = Query(SystemPerformanceInformation, 512);
        if (data is null || data.Length < 112)
            return default;
        return new PerformanceBlock(
            ReadU32(data, 32), ReadU32(data, 36), ReadU32(data, 40),
            ReadI64(data, 8), ReadI64(data, 16), ReadI64(data, 24),
            ReadU32(data, 60), ReadU32(data, 80), ReadU32(data, 96), ReadU32(data, 104));
    }

    private static (long ContextSwitches, long Interrupts, long Dpcs) ReadInterruptTotals()
    {
        long context = 0, dpcs = 0;
        var interrupts = Query(SystemInterruptInformation, 16 * Math.Max(1, Environment.ProcessorCount));
        if (interrupts is not null)
        {
            for (var offset = 0; offset + 24 <= interrupts.Length; offset += 24)
            {
                context += ReadU32(interrupts, offset);
                dpcs += ReadU32(interrupts, offset + 4);
            }
        }

        long irq = 0;
        var processors = Query(SystemProcessorPerformanceInformation, 48 * Math.Max(1, Environment.ProcessorCount));
        if (processors is not null)
        {
            var stride = processors.Length >= 56 ? 56 : 48;
            for (var offset = 0; offset + 44 <= processors.Length; offset += stride)
                irq += ReadU32(processors, offset + 40);
        }

        return (context, irq, dpcs);
    }

    private static MemoryLists ReadMemoryLists()
    {
        var data = Query(SystemMemoryListInformation, 200);
        if (data is null || data.Length < 13 * nint.Size)
            return default;
        var size = nint.Size;
        nint At(int index) => size == 8 ? (nint)BitConverter.ToInt64(data, index * 8) : BitConverter.ToInt32(data, index * 4);
        var standby = At(5).ToInt64() + At(6).ToInt64() + At(7).ToInt64() + At(8).ToInt64()
                      + At(9).ToInt64() + At(10).ToInt64() + At(11).ToInt64() + At(12).ToInt64();
        return new MemoryLists(
            At(0).ToInt64(), At(1).ToInt64(), At(2).ToInt64(), At(3).ToInt64(), standby,
            At(5).ToInt64(), At(6).ToInt64(), At(7).ToInt64(), At(8).ToInt64(),
            At(9).ToInt64(), At(10).ToInt64(), At(11).ToInt64(), At(12).ToInt64(),
            data.Length >= 22 * size ? At(21).ToInt64() : 0);
    }

    private static (int Cores, int Sockets, int Logical) ReadTopology()
    {
        var logical = Math.Max(1, Environment.ProcessorCount);
        var length = 0;
        GetLogicalProcessorInformationEx(RelationAll, nint.Zero, ref length);
        if (length <= 0)
            return (logical, 1, logical);
        var buffer = Marshal.AllocHGlobal(length);
        try
        {
            var size = length;
            if (!GetLogicalProcessorInformationEx(RelationAll, buffer, ref size))
                return (logical, 1, logical);
            var cores = 0;
            var sockets = 0;
            var offset = 0;
            while (offset + 8 <= size)
            {
                var relationship = Marshal.ReadInt32(buffer, offset);
                var block = Marshal.ReadInt32(buffer, offset + 4);
                if (block <= 0) break;
                if (relationship == RelationProcessorCore) cores++;
                else if (relationship == RelationProcessorPackage) sockets++;
                offset += block;
            }
            return (cores > 0 ? cores : logical, sockets > 0 ? sockets : 1, logical);
        }
        catch { return (logical, 1, logical); }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static byte[]? Query(int cls, int hint)
    {
        var length = Math.Max(hint, 64);
        for (var attempt = 0; attempt < 4; attempt++)
        {
            var buffer = Marshal.AllocHGlobal(length);
            try
            {
                var status = NtQuerySystemInformation(cls, buffer, length, out var returned);
                if (status == 0)
                {
                    var data = new byte[returned > 0 ? returned : length];
                    Marshal.Copy(buffer, data, 0, data.Length);
                    return data;
                }
                if (status == unchecked((int)0xC0000004) && returned > length) { length = returned; continue; }
                if (status == unchecked((int)0xC0000004)) { length *= 2; continue; }
                return null;
            }
            finally { Marshal.FreeHGlobal(buffer); }
        }
        return null;
    }

    private static long ReadI64(byte[] data, int offset)
        => offset + 8 <= data.Length ? BitConverter.ToInt64(data, offset) : 0;

    private static long ReadU32(byte[] data, int offset)
        => offset + 4 <= data.Length ? BitConverter.ToUInt32(data, offset) : 0;

    private readonly record struct PerformanceBlock(
        long ReadOps, long WriteOps, long OtherOps,
        long ReadBytes, long WriteBytes, long OtherBytes,
        long PageFaults, long PageReads, long DirtyWrites, long MappedWrites);

    private readonly record struct MemoryLists(
        long Zeroed, long Free, long Modified, long ModifiedNoWrite, long Standby,
        long P0, long P1, long P2, long P3, long P4, long P5, long P6, long P7,
        long PagedFileModified);
}

internal readonly record struct SystemTimes(ulong Idle, ulong Kernel, ulong User);
