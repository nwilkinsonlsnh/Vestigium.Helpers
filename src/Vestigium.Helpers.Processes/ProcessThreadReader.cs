using System.Diagnostics;
using System.Runtime.InteropServices;

namespace Vestigium.Helpers.Processes;

internal static class ProcessThreadReader
{
    private const uint ThreadQueryLimitedInformation = 0x0800;
    private const int ThreadQuerySetWin32StartAddress = 9;
    private const int ThreadIoPriority = 22;
    private const int ThreadPagePriority = 24;

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern nint OpenThread(uint access, bool inherit, int threadId);

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationThread(nint handle, int infoClass, out nint startAddress, int length, out int returned);

    [DllImport("ntdll.dll")]
    private static extern int NtQueryInformationThread(nint handle, int infoClass, out uint value, int length, out int returned);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool QueryThreadCycleTime(nint handle, out ulong cycles);

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool GetThreadIdealProcessorEx(nint handle, out ProcessorNumber number);

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessorNumber
    {
        public ushort Group;
        public byte Number;
        public byte Reserved;
    }

    internal static IReadOnlyList<ThreadInfo> Capture(int pid, bool includeStack)
    {
        Process process;
        try { process = Process.GetProcessById(pid); }
        catch { return []; }

        ProcessThreadCollection raw;
        try { raw = process.Threads; }
        catch
        {
            process.Dispose();
            return [];
        }

        var rows = new List<ThreadInfo>(raw.Count);
        IReadOnlyList<(string Name, nint Base, int Size)> modules = includeStack ? ReadModules(process) : [];
        foreach (ProcessThread thread in raw)
        {
            try { rows.Add(Read(pid, thread, includeStack, modules)); }
            catch { }
            finally { thread.Dispose(); }
        }

        process.Dispose();
        rows.Sort((a, b) => a.ThreadId.CompareTo(b.ThreadId));
        return rows;
    }

    private static ThreadInfo Read(int pid, ProcessThread thread, bool includeStack, IReadOnlyList<(string Name, nint Base, int Size)> modules)
    {
        DateTimeOffset? start = null;
        try { start = new DateTimeOffset(thread.StartTime); } catch { }

        ThreadState state;
        try { state = MapState(thread.ThreadState); }
        catch { state = ThreadState.Unknown; }

        string? wait = null;
        try { if (state == ThreadState.Waiting) wait = thread.WaitReason.ToString(); } catch { }

        TimeSpan? kernel = null, user = null;
        try { kernel = thread.PrivilegedProcessorTime; } catch { }
        try { user = thread.UserProcessorTime; } catch { }

        int? basePri = null, dynPri = null;
        try { basePri = thread.BasePriority; } catch { }
        try { dynPri = thread.CurrentPriority; } catch { }

        string? startAddress = null, startModule = null, stack = null;
        long? cycles = null;
        int? ideal = null, ioPri = null, memPri = null;

        var handle = OpenThread(ThreadQueryLimitedInformation, false, thread.Id);
        if (handle != 0 && handle != nint.Zero)
        {
            try
            {
                if (NtQueryInformationThread(handle, ThreadQuerySetWin32StartAddress, out nint address, nint.Size, out _) == 0 && address != 0)
                {
                    startAddress = "0x" + address.ToInt64().ToString("X");
                    if (includeStack)
                    {
                        startModule = ModuleAt(modules, address);
                        if (startModule is not null)
                            stack = startModule + "+" + Offset(modules, address);
                    }
                }

                if (QueryThreadCycleTime(handle, out var cyc))
                    cycles = unchecked((long)cyc);
                if (GetThreadIdealProcessorEx(handle, out var proc))
                    ideal = proc.Number;
                if (NtQueryInformationThread(handle, ThreadIoPriority, out uint io, 4, out _) == 0)
                    ioPri = (int)io;
                if (NtQueryInformationThread(handle, ThreadPagePriority, out uint mem, 4, out _) == 0)
                    memPri = (int)mem;
            }
            finally { NativeMethods.CloseHandle(handle); }
        }

        return new ThreadInfo
        {
            ThreadId = thread.Id,
            ProcessId = pid,
            StartTime = start,
            State = state,
            WaitReason = wait,
            StartAddress = startAddress,
            StartModule = startModule,
            Stack = stack,
            KernelTime = kernel,
            UserTime = user,
            Cycles = cycles,
            BasePriority = basePri,
            DynamicPriority = dynPri,
            IoPriority = ioPri,
            MemoryPriority = memPri,
            IdealProcessor = ideal
        };
    }

    private static ThreadState MapState(System.Diagnostics.ThreadState state) => state switch
    {
        System.Diagnostics.ThreadState.Initialized => ThreadState.Initialized,
        System.Diagnostics.ThreadState.Ready => ThreadState.Ready,
        System.Diagnostics.ThreadState.Running => ThreadState.Running,
        System.Diagnostics.ThreadState.Standby => ThreadState.Standby,
        System.Diagnostics.ThreadState.Terminated => ThreadState.Terminated,
        System.Diagnostics.ThreadState.Wait => ThreadState.Waiting,
        System.Diagnostics.ThreadState.Transition => ThreadState.Transition,
        _ => ThreadState.Unknown
    };

    private static IReadOnlyList<(string Name, nint Base, int Size)> ReadModules(Process process)
    {
        var list = new List<(string, nint, int)>();
        try
        {
            foreach (ProcessModule module in process.Modules)
            {
                try { list.Add((module.ModuleName, module.BaseAddress, module.ModuleMemorySize)); }
                catch { }
                finally { module.Dispose(); }
            }
        }
        catch { }
        return list;
    }

    private static string? ModuleAt(IReadOnlyList<(string Name, nint Base, int Size)> modules, nint address)
    {
        var value = address.ToInt64();
        foreach (var module in modules)
        {
            var start = module.Base.ToInt64();
            if (value >= start && value < start + module.Size)
                return module.Name;
        }
        return null;
    }

    private static string Offset(IReadOnlyList<(string Name, nint Base, int Size)> modules, nint address)
    {
        var value = address.ToInt64();
        foreach (var module in modules)
        {
            var start = module.Base.ToInt64();
            if (value >= start && value < start + module.Size)
                return "0x" + (value - start).ToString("X");
        }
        return "0x0";
    }
}
