using System.Runtime.InteropServices;
using Vestigium.Helpers.SystemInfo;

namespace Vestigium.Helpers.SystemInfo.Memory;

/// <summary>One memory read. A failed API leaves only its fields Unavailable.</summary>
public readonly record struct MemoryRead(
    Fact<ulong> TotalBytes,
    Fact<ulong> AvailableBytes,
    Fact<ulong> InUseBytes,
    Fact<ulong> CommitPeakBytes,
    Fact<ulong> PagedBytes,
    Fact<ulong> NonpagedBytes);

/// <summary>Physical and commit snapshot. Not the PDH Memory series.</summary>
public static class MemoryFacts
{
    public static MemoryRead Read()
    {
        var physical = ReadPhysical();
        var pools = ReadPools();
        return new MemoryRead(
            physical.Total,
            physical.Available,
            physical.InUse,
            pools.Peak,
            pools.Paged,
            pools.Nonpaged);
    }

    private static (Fact<ulong> Total, Fact<ulong> Available, Fact<ulong> InUse) ReadPhysical()
    {
        var mem = new MemoryStatusEx { Length = (uint)Marshal.SizeOf<MemoryStatusEx>() };
        if (!GlobalMemoryStatusEx(ref mem) || mem.TotalPhys == 0)
            return (Fact<ulong>.Unavailable(), Fact<ulong>.Unavailable(), Fact<ulong>.Unavailable());
        var used = mem.TotalPhys > mem.AvailPhys ? mem.TotalPhys - mem.AvailPhys : 0;
        return (Fact<ulong>.Ok(mem.TotalPhys), Fact<ulong>.Ok(mem.AvailPhys), Fact<ulong>.Ok(used));
    }

    private static (Fact<ulong> Peak, Fact<ulong> Paged, Fact<ulong> Nonpaged) ReadPools()
    {
        var perf = new PerformanceInformation { Size = Marshal.SizeOf<PerformanceInformation>() };
        if (!GetPerformanceInfo(out perf, perf.Size) || perf.PageSize == 0)
            return (Fact<ulong>.Unavailable(), Fact<ulong>.Unavailable(), Fact<ulong>.Unavailable());
        var page = (ulong)perf.PageSize.ToInt64();
        return (
            Fact<ulong>.Ok((ulong)Math.Max(0, perf.CommitPeak.ToInt64()) * page),
            Fact<ulong>.Ok((ulong)Math.Max(0, perf.KernelPaged.ToInt64()) * page),
            Fact<ulong>.Ok((ulong)Math.Max(0, perf.KernelNonpaged.ToInt64()) * page));
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GlobalMemoryStatusEx(ref MemoryStatusEx status);

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool GetPerformanceInfo(out PerformanceInformation info, int size);

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
}
