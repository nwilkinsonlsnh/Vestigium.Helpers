using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Vestigium.Helpers.Processes;

internal static class ProcessSnapshotter
{
    internal static IReadOnlyList<ProcessInfo> Capture(ProcessDetailLevel level)
    {
        Process[] raw;
        try { raw = Process.GetProcesses(); }
        catch { return []; }

        var pids = new HashSet<int>();
        foreach (var process in raw)
        {
            try { pids.Add(process.Id); }
            catch { }
        }

        var rows = new List<ProcessInfo>(raw.Length);
        foreach (var process in raw)
        {
            try
            {
                var row = Read(process, level, pids);
                if (row is not null)
                    rows.Add(row);
            }
            catch { }
            finally { process.Dispose(); }
        }

        rows.Sort((a, b) => a.Pid.CompareTo(b.Pid));
        return rows;
    }

    internal static ProcessInfo? CapturePid(int pid, ProcessDetailLevel level)
    {
        Process process;
        try { process = Process.GetProcessById(pid); }
        catch (ArgumentException) { return null; }
        catch (InvalidOperationException) { return null; }
        catch (System.ComponentModel.Win32Exception) { return null; }

        try { return Read(process, level, LivePids()); }
        catch (InvalidOperationException) { return null; }
        finally { process.Dispose(); }
    }

    private static HashSet<int> LivePids()
    {
        var set = new HashSet<int>();
        Process[] raw;
        try { raw = Process.GetProcesses(); }
        catch { return set; }
        foreach (var process in raw)
        {
            try { set.Add(process.Id); }
            catch { }
            finally { process.Dispose(); }
        }
        return set;
    }

    private static ProcessInfo? Read(Process process, ProcessDetailLevel level, HashSet<int> livePids)
    {
        int pid;
        try { pid = process.Id; }
        catch { return null; }
        if (pid <= 0)
            return null;

        var missing = new List<FieldAvailability>();
        var name = ReadName(process);
        var handle = NativeMethods.OpenProcess(NativeMethods.ProcessQueryLimitedInformation, false, pid);
        try
        {
            int? parentPid = null;
            if (handle != 0 && handle != nint.Zero)
            {
                parentPid = ReadParentPid(handle);
                if (parentPid is null)
                    missing.Add(new FieldAvailability(ProcessField.ParentPid, Availability.Denied, "NtQueryInformationProcess"));
            }
            else
            {
                missing.Add(new FieldAvailability(ProcessField.ParentPid, Availability.Denied, "OpenProcess"));
            }

            bool? parentAlive = parentPid is int ppid && ppid > 0
                ? livePids.Contains(ppid)
                : parentPid is 0 ? true : null;

            int? sessionId = null;
            try { sessionId = process.SessionId; }
            catch { missing.Add(new FieldAvailability(ProcessField.SessionId, Availability.Denied, "SessionId")); }

            string? imagePath = null;
            if (level != ProcessDetailLevel.Identity)
            {
                imagePath = handle != 0 && handle != nint.Zero ? ReadImagePath(handle) : null;
                if (imagePath is null)
                    missing.Add(new FieldAvailability(ProcessField.ImagePath, Availability.Denied, "QueryFullProcessImageName"));
            }

            TimeSpan? cpuTime = null;
            long? privateBytes = null;
            long? workingSet = null;
            long? ioReads = null;
            long? ioReadBytes = null;
            long? ioWrites = null;
            long? ioWriteBytes = null;
            double? gpuUse = null;
            long? gpuDed = null;
            long? gpuSys = null;

            if (level != ProcessDetailLevel.Identity)
            {
                try { cpuTime = process.TotalProcessorTime; }
                catch { missing.Add(new FieldAvailability(ProcessField.CpuTime, Availability.Denied, "TotalProcessorTime")); }
                missing.Add(new FieldAvailability(ProcessField.CpuPercent, Availability.Unsupported, "requires watcher"));
                try { privateBytes = process.PrivateMemorySize64; }
                catch { missing.Add(new FieldAvailability(ProcessField.PrivateBytes, Availability.Denied, "PrivateMemorySize64")); }
                try { workingSet = process.WorkingSet64; }
                catch { missing.Add(new FieldAvailability(ProcessField.WorkingSet, Availability.Denied, "WorkingSet64")); }
                if (handle != 0 && handle != nint.Zero && NativeMethods.GetProcessIoCounters(handle, out var io))
                {
                    ioReads = checked((long)io.ReadOperationCount);
                    ioReadBytes = checked((long)io.ReadTransferCount);
                    ioWrites = checked((long)io.WriteOperationCount);
                    ioWriteBytes = checked((long)io.WriteTransferCount);
                }
                else
                {
                    missing.Add(new FieldAvailability(ProcessField.IoReads, Availability.Denied, "GetProcessIoCounters"));
                    missing.Add(new FieldAvailability(ProcessField.IoReadBytes, Availability.Denied, "GetProcessIoCounters"));
                    missing.Add(new FieldAvailability(ProcessField.IoWrites, Availability.Denied, "GetProcessIoCounters"));
                    missing.Add(new FieldAvailability(ProcessField.IoWriteBytes, Availability.Denied, "GetProcessIoCounters"));
                }

                var gpu = ProcessGpuCatalog.ForPid(pid);
                gpuUse = gpu.UsagePercent;
                gpuDed = gpu.DedicatedBytes;
                gpuSys = gpu.SystemBytes;
                if (!ProcessGpuCatalog.Supported)
                {
                    missing.Add(new FieldAvailability(ProcessField.GpuUsagePercent, Availability.Unsupported, "GPU counters"));
                    missing.Add(new FieldAvailability(ProcessField.GpuDedicatedBytes, Availability.Unsupported, "GPU counters"));
                    missing.Add(new FieldAvailability(ProcessField.GpuSystemBytes, Availability.Unsupported, "GPU counters"));
                }
            }

            if (string.IsNullOrWhiteSpace(name))
                name = imagePath is null ? $"pid-{pid}" : Path.GetFileName(imagePath);

            var row = new ProcessInfo
            {
                Pid = pid,
                ParentPid = parentPid,
                ParentAlive = parentAlive,
                Name = name,
                SessionId = sessionId,
                ImagePath = imagePath,
                ImageType = ProcessImageType.Unknown,
                CpuTime = cpuTime,
                CpuPercent = null,
                PrivateBytes = privateBytes,
                WorkingSet = workingSet,
                IoReads = ioReads,
                IoReadBytes = ioReadBytes,
                IoWrites = ioWrites,
                IoWriteBytes = ioWriteBytes,
                GpuUsagePercent = gpuUse,
                GpuDedicatedBytes = gpuDed,
                GpuSystemBytes = gpuSys,
                Availability = missing
            };

            if (level == ProcessDetailLevel.Full && handle != 0 && handle != nint.Zero)
            {
                ProcessFullReader.Fill(process, handle, row, missing);
                row.Availability = missing;
            }

            return row;
        }
        finally
        {
            if (handle != 0 && handle != nint.Zero)
                _ = NativeMethods.CloseHandle(handle);
        }
    }

    private static string ReadName(Process process)
    {
        try
        {
            var name = process.ProcessName;
            if (string.IsNullOrWhiteSpace(name))
                return $"pid-{process.Id}";
            if (name.Equals("Idle", StringComparison.OrdinalIgnoreCase)
                || name.Equals("System", StringComparison.OrdinalIgnoreCase)
                || name.Contains('.', StringComparison.Ordinal))
                return name;
            return name + ".exe";
        }
        catch
        {
            return $"pid-{process.Id}";
        }
    }

    private static int? ReadParentPid(nint handle)
    {
        var status = NativeMethods.NtQueryProcessBasicInfo(
            handle,
            NativeMethods.ProcessBasicInformationClass,
            out NativeMethods.ProcessBasicInfo info,
            Marshal.SizeOf<NativeMethods.ProcessBasicInfo>(),
            out _);
        if (status != 0)
            return null;
        var parent = info.InheritedFromUniqueProcessId.ToInt64();
        if (parent < 0 || parent > int.MaxValue)
            return null;
        return (int)parent;
    }

    private static string? ReadImagePath(nint handle)
    {
        var size = 32768;
        var buffer = new StringBuilder(size);
        var cap = size;
        if (!NativeMethods.QueryFullProcessImageName(handle, 0, buffer, ref cap))
            return null;
        var path = buffer.ToString();
        return string.IsNullOrWhiteSpace(path) ? null : path;
    }
}
