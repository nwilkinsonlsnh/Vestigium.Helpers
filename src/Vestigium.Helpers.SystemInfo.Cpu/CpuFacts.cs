using System.Runtime.InteropServices;
using Vestigium.Helpers.SystemInfo;

namespace Vestigium.Helpers.SystemInfo.Cpu;

/// <summary>
/// Processor topology and census. A failed native call is Unavailable, not a zero clock.
/// </summary>
public static class CpuFacts
{
    private static readonly Fact<CpuTopology> Topology = ReadTopology();
    private static Fact<CpuLive> _live = Fact<CpuLive>.Unavailable();
    private static DateTimeOffset _liveAt;
    private static readonly object Gate = new();

    public static Fact<CpuTopology> Host => Topology;

    public static Fact<CpuLive> Live()
    {
        lock (Gate)
        {
            if (DateTimeOffset.UtcNow - _liveAt < TimeSpan.FromSeconds(1) && _live.IsOk)
                return _live;
            _live = ReadLive();
            _liveAt = DateTimeOffset.UtcNow;
            return _live;
        }
    }

    private static Fact<CpuLive> ReadLive()
    {
        var perf = new PerformanceInformation { Size = Marshal.SizeOf<PerformanceInformation>() };
        if (!GetPerformanceInfo(out perf, perf.Size))
            return Fact<CpuLive>.Unavailable();
        if (!TrySpeeds(out var current, out var max))
            return Fact<CpuLive>.Unavailable();
        return Fact<CpuLive>.Ok(new CpuLive(perf.ProcessCount, perf.ThreadCount, perf.HandleCount, current, max));
    }

    private static bool TrySpeeds(out uint current, out uint max)
    {
        current = 0;
        max = 0;
        var count = Math.Max(1, Environment.ProcessorCount);
        var size = Marshal.SizeOf<ProcessorPowerInformation>() * count;
        var buffer = Marshal.AllocHGlobal(size);
        try
        {
            if (CallNtPowerInformation(11, nint.Zero, 0, buffer, size) != 0)
                return false;
            for (var i = 0; i < count; i++)
            {
                var row = Marshal.PtrToStructure<ProcessorPowerInformation>(buffer + (i * Marshal.SizeOf<ProcessorPowerInformation>()));
                if (row.CurrentMhz > current)
                    current = row.CurrentMhz;
                if (row.MaxMhz > max)
                    max = row.MaxMhz;
            }

            if (current == 0)
                current = max;
            return max > 0;
        }
        catch (DllNotFoundException)
        {
            return false;
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private static Fact<CpuTopology> ReadTopology()
    {
        var length = 0;
        GetLogicalProcessorInformationEx(RelationAll, nint.Zero, ref length);
        if (length <= 0)
            return Fact<CpuTopology>.Unavailable();

        var buffer = Marshal.AllocHGlobal(length);
        try
        {
            var size = length;
            if (!GetLogicalProcessorInformationEx(RelationAll, buffer, ref size))
                return Fact<CpuTopology>.Unavailable();

            var cores = 0;
            var sockets = 0;
            var l1 = 0L;
            var l2 = 0L;
            var l3 = 0L;
            var l4 = 0L;
            var offset = 0;
            while (offset + 8 <= size)
            {
                var relationship = Marshal.ReadInt32(buffer, offset);
                var block = Marshal.ReadInt32(buffer, offset + 4);
                if (block <= 0)
                    break;
                if (relationship == RelationProcessorCore)
                    cores++;
                else if (relationship == RelationProcessorPackage)
                    sockets++;
                else if (relationship == RelationCache && offset + 16 <= size)
                {
                    var level = Marshal.ReadByte(buffer, offset + 8);
                    var cacheSize = Marshal.ReadInt32(buffer, offset + 12);
                    if (cacheSize > 0)
                    {
                        switch (level)
                        {
                            case 1: l1 += cacheSize; break;
                            case 2: l2 += cacheSize; break;
                            case 3: l3 += cacheSize; break;
                            default: if (level >= 4) l4 += cacheSize; break;
                        }
                    }
                }

                offset += block;
            }

            if (sockets <= 0 || cores <= 0)
                return Fact<CpuTopology>.Unavailable();
            return Fact<CpuTopology>.Ok(new CpuTopology(
                sockets,
                cores,
                Math.Max(1, Environment.ProcessorCount),
                l1,
                l2,
                l3,
                l4));
        }
        catch (DllNotFoundException)
        {
            return Fact<CpuTopology>.Unavailable();
        }
        finally
        {
            Marshal.FreeHGlobal(buffer);
        }
    }

    private const int RelationProcessorCore = 0;
    private const int RelationCache = 2;
    private const int RelationProcessorPackage = 3;
    private const int RelationAll = 0xFFFF;

    [DllImport("psapi.dll", SetLastError = true)]
    private static extern bool GetPerformanceInfo(out PerformanceInformation info, int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetLogicalProcessorInformationEx(int relationship, nint buffer, ref int length);

    [DllImport("powrprof.dll")]
    private static extern uint CallNtPowerInformation(int level, nint input, int inputSize, nint output, int outputSize);

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

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessorPowerInformation
    {
        public uint Number;
        public uint MaxMhz;
        public uint CurrentMhz;
        public uint MhzLimit;
        public uint MaxIdleState;
        public uint CurrentIdleState;
    }
}
