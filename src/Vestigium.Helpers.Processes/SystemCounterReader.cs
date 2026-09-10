using System.Runtime.InteropServices;

namespace Vestigium.Helpers.Processes;

internal static class SystemCounterReader
{
    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetSystemTimes(out FileTime idle, out FileTime kernel, out FileTime user);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool GetPerformanceInfo(out PerformanceInformation info, int size);

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

    internal static SystemCounters Capture(SystemTimes? previous, TimeSpan? interval)
    {
        double? cpu = null;
        var times = ReadTimes();
        if (previous is { } prior && times is { } current && interval is not null)
        {
            var idle = Diff(current.Idle, prior.Idle);
            var kernel = Diff(current.Kernel, prior.Kernel);
            var user = Diff(current.User, prior.User);
            var total = kernel + user;
            if (total > 0)
                cpu = 100.0 * (total - idle) / total;
        }

        long? physTotal = null, physAvail = null, commitCurrent = null, commitLimit = null, commitPeak = null;
        var mem = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (GlobalMemoryStatusEx(ref mem))
        {
            physTotal = checked((long)mem.TotalPhys);
            physAvail = checked((long)mem.AvailPhys);
            commitLimit = checked((long)mem.TotalPageFile);
            commitCurrent = checked((long)(mem.TotalPageFile - mem.AvailPageFile));
        }

        int? processes = null, threads = null, handles = null;
        var perf = new PerformanceInformation { Size = Marshal.SizeOf<PerformanceInformation>() };
        if (GetPerformanceInfo(out perf, perf.Size))
        {
            processes = perf.ProcessCount;
            threads = perf.ThreadCount;
            handles = perf.HandleCount;
            var page = perf.PageSize == 0 ? 4096L : perf.PageSize.ToInt64();
            commitPeak = perf.CommitPeak.ToInt64() * page;
        }

        double? commitPct = commitCurrent is long cur && commitLimit is long lim && lim > 0 ? (double)cur / lim : null;
        double? physPct = physTotal is long tot && physAvail is long av && tot > 0 ? (double)(tot - av) / tot : null;

        return new SystemCounters
        {
            Timestamp = DateTimeOffset.Now,
            Interval = interval,
            CpuPercent = cpu,
            SystemCommitPercent = commitPct,
            PhysicalMemoryPercent = physPct,
            CommitCurrent = commitCurrent,
            CommitLimit = commitLimit,
            CommitPeak = commitPeak,
            CommitCurrentToLimit = commitPct,
            CommitPeakToLimit = commitPeak is long peak && commitLimit is long lim2 && lim2 > 0 ? (double)peak / lim2 : null,
            PhysicalTotal = physTotal,
            PhysicalAvailable = physAvail,
            ProcessCount = processes,
            ThreadCount = threads,
            HandleCount = handles,
            LogicalProcessors = Environment.ProcessorCount,
            Cores = Environment.ProcessorCount,
            Sockets = 1
        };
    }

    internal static SystemTimes? ReadTimes()
    {
        if (!GetSystemTimes(out var idle, out var kernel, out var user))
            return null;
        return new SystemTimes(idle.Ticks, kernel.Ticks, user.Ticks);
    }

    private static ulong Diff(ulong now, ulong then) => now >= then ? now - then : 0;
}

internal readonly record struct SystemTimes(ulong Idle, ulong Kernel, ulong User);
