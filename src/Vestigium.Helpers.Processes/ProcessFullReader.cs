using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace Vestigium.Helpers.Processes;

internal static class ProcessFullReader
{
    internal static void Fill(Process process, nint handle, ProcessInfo row, List<FieldAvailability> missing)
    {
        if (handle == 0 || handle == nint.Zero)
            return;

        row.ImageType = ProcessImageReader.ReadType(handle, row.ImagePath);
        ProcessImageReader.ReadVersion(row.ImagePath, out var description, out var company, out var version);
        row.Description = description;
        row.CompanyName = company;
        row.Version = version;
        row.VerifiedSigner = ProcessSigner.Read(row.ImagePath);
        row.PackageName = ReadPackage(handle);
        row.CommandLine = ReadCommandLine(row.Pid, missing);
        ReadWindow(process, row);
        ReadToken(handle, row, missing);
        ReadMitigations(handle, row, missing);
        ReadProtection(handle, row);
        ReadDpi(handle, row, missing);
        row.AutostartLocation = ProcessAutostart.Locate(row.ImagePath, row.Name);
        row.Comment = ProcessCommentStore.Get(ProcessCommentStore.Key(row.ImagePath, row.Name));
    }

    private static string? ReadPackage(nint handle)
    {
        var length = 0;
        _ = NativeMethods.GetPackageFamilyName(handle, ref length, null);
        if (length <= 1)
            return null;
        var buffer = new StringBuilder(length);
        var status = NativeMethods.GetPackageFamilyName(handle, ref length, buffer);
        return status == 0 && !string.IsNullOrWhiteSpace(buffer.ToString()) ? buffer.ToString() : null;
    }

    private static string? ReadCommandLine(int pid, List<FieldAvailability> missing)
    {
        var vm = NativeMethods.OpenProcess(
            NativeMethods.ProcessQueryLimitedInformation | NativeMethods.ProcessVmRead,
            false,
            pid);
        if (vm == 0 || vm == nint.Zero)
        {
            missing.Add(new FieldAvailability(ProcessField.CommandLine, Availability.Denied, "PROCESS_VM_READ"));
            return null;
        }

        try
        {
            var status = NativeMethods.NtQueryProcessBasicInfo(
                vm,
                NativeMethods.ProcessBasicInformationClass,
                out NativeMethods.ProcessBasicInfo info,
                Marshal.SizeOf<NativeMethods.ProcessBasicInfo>(),
                out _);
            if (status != 0 || info.PebBaseAddress == 0)
            {
                missing.Add(new FieldAvailability(ProcessField.CommandLine, Availability.Denied, "PEB"));
                return null;
            }

            var ptrSize = nint.Size;
            var parameters = ReadPointer(vm, info.PebBaseAddress + (ptrSize == 8 ? 0x20 : 0x10), ptrSize);
            if (parameters == 0)
            {
                missing.Add(new FieldAvailability(ProcessField.CommandLine, Availability.Denied, "ProcessParameters"));
                return null;
            }

            var commandLineOffset = ptrSize == 8 ? 0x70 : 0x40;
            var unicodeSize = ptrSize == 8 ? 16 : 8;
            var unicode = ReadBytes(vm, parameters + commandLineOffset, unicodeSize);
            if (unicode is null)
            {
                missing.Add(new FieldAvailability(ProcessField.CommandLine, Availability.Denied, "CommandLine"));
                return null;
            }

            var length = BitConverter.ToUInt16(unicode, 0);
            var buffer = ptrSize == 8 ? BitConverter.ToInt64(unicode, 8) : BitConverter.ToInt32(unicode, 4);
            if (length == 0 || buffer == 0)
                return null;
            var text = ReadBytes(vm, (nint)buffer, length);
            return text is null ? null : Encoding.Unicode.GetString(text);
        }
        catch
        {
            missing.Add(new FieldAvailability(ProcessField.CommandLine, Availability.Denied, "ReadProcessMemory"));
            return null;
        }
        finally
        {
            NativeMethods.CloseHandle(vm);
        }
    }

    private static nint ReadPointer(nint handle, nint address, int ptrSize)
    {
        var raw = ReadBytes(handle, address, ptrSize);
        if (raw is null)
            return 0;
        return ptrSize == 8 ? (nint)BitConverter.ToInt64(raw, 0) : BitConverter.ToInt32(raw, 0);
    }

    private static byte[]? ReadBytes(nint handle, nint address, int size)
    {
        if (size <= 0 || size > 64 * 1024)
            return null;
        var buffer = new byte[size];
        if (!NativeMethods.ReadProcessMemory(handle, address, buffer, size, out var read) || read.ToInt64() != size)
            return null;
        return buffer;
    }

    private static void ReadWindow(Process process, ProcessInfo row)
    {
        nint hwnd;
        try { hwnd = process.MainWindowHandle; }
        catch { return; }
        if (hwnd == 0)
        {
            row.WindowStatus = WindowStatus.None;
            return;
        }

        try
        {
            if (NativeMethods.IsHungAppWindow(hwnd))
                row.WindowStatus = WindowStatus.Hung;
            else if (NativeMethods.IsIconic(hwnd))
                row.WindowStatus = WindowStatus.Minimized;
            else if (NativeMethods.IsZoomed(hwnd))
                row.WindowStatus = WindowStatus.Maximized;
            else if (NativeMethods.IsWindowVisible(hwnd))
                row.WindowStatus = WindowStatus.Visible;
            else
                row.WindowStatus = WindowStatus.Hidden;
        }
        catch { row.WindowStatus = WindowStatus.None; }

        try { row.WindowTitle = string.IsNullOrWhiteSpace(process.MainWindowTitle) ? null : process.MainWindowTitle; }
        catch { row.WindowTitle = null; }
    }

    private static void ReadToken(nint handle, ProcessInfo row, List<FieldAvailability> missing)
    {
        if (!NativeMethods.OpenProcessToken(handle, NativeMethods.TokenQuery, out var token) || token == 0)
        {
            missing.Add(new FieldAvailability(ProcessField.IntegrityLevel, Availability.Denied, "OpenProcessToken"));
            return;
        }

        try
        {
            row.IntegrityLevel = ReadIntegrity(token);
            var ui = ReadTokenDword(token, NativeMethods.TokenUiAccess);
            row.UiAccess = ui is null ? null : ui != 0;
            var virt = ReadTokenDword(token, NativeMethods.TokenVirtualizationEnabled);
            row.Virtualized = virt is null ? null : virt != 0;
        }
        finally { NativeMethods.CloseHandle(token); }
    }

    private static IntegrityLevel? ReadIntegrity(nint token)
    {
        NativeMethods.GetTokenInformation(token, NativeMethods.TokenIntegrityLevel, nint.Zero, 0, out var needed);
        if (needed <= 0)
            return null;
        var buffer = Marshal.AllocHGlobal(needed);
        try
        {
            if (!NativeMethods.GetTokenInformation(token, NativeMethods.TokenIntegrityLevel, buffer, needed, out _))
                return null;
            var sid = Marshal.ReadIntPtr(buffer);
            var subCount = Marshal.ReadByte(sid, 1);
            var rid = Marshal.ReadInt32(sid, 8 + ((subCount - 1) * 4));
            return rid switch
            {
                0x0000 => IntegrityLevel.Untrusted,
                0x1000 => IntegrityLevel.Low,
                0x2000 => IntegrityLevel.Medium,
                0x2100 => IntegrityLevel.MediumPlus,
                0x3000 => IntegrityLevel.High,
                0x4000 => IntegrityLevel.System,
                0x5000 => IntegrityLevel.Protected,
                _ => null
            };
        }
        catch { return null; }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static int? ReadTokenDword(nint token, int kind)
    {
        var buffer = Marshal.AllocHGlobal(4);
        try
        {
            if (!NativeMethods.GetTokenInformation(token, kind, buffer, 4, out _))
                return null;
            return Marshal.ReadInt32(buffer);
        }
        catch { return null; }
        finally { Marshal.FreeHGlobal(buffer); }
    }

    private static void ReadMitigations(nint handle, ProcessInfo row, List<FieldAvailability> missing)
    {
        uint dep = 0;
        if (NativeMethods.GetProcessMitigationPolicy(handle, 0, ref dep, sizeof(uint)))
        {
            if ((dep & 4) != 0) row.DepStatus = DepStatus.Permanent;
            else if ((dep & 1) != 0) row.DepStatus = DepStatus.Enabled;
            else row.DepStatus = DepStatus.Disabled;
        }
        else
        {
            row.DepStatus = DepStatus.Unknown;
            missing.Add(new FieldAvailability(ProcessField.DepStatus, Availability.Unsupported, "GetProcessMitigationPolicy"));
        }

        uint aslr = 0;
        if (NativeMethods.GetProcessMitigationPolicy(handle, 1, ref aslr, sizeof(uint)))
            row.AslrEnabled = (aslr & 3) != 0;
        else
            missing.Add(new FieldAvailability(ProcessField.AslrEnabled, Availability.Unsupported, "ASLR"));

        uint cfg = 0;
        if (NativeMethods.GetProcessMitigationPolicy(handle, 7, ref cfg, sizeof(uint)))
        {
            if ((cfg & 2) != 0) row.ControlFlowGuard = MitigationState.ExportSuppressed;
            else if ((cfg & 1) != 0) row.ControlFlowGuard = MitigationState.Enabled;
            else row.ControlFlowGuard = MitigationState.Disabled;
        }
        else row.ControlFlowGuard = MitigationState.Unknown;

        uint shadow = 0;
        if (NativeMethods.GetProcessMitigationPolicy(handle, 15, ref shadow, sizeof(uint)))
            row.StackProtection = (shadow & 1) != 0 ? MitigationState.Enabled : MitigationState.Disabled;
        else
            row.StackProtection = MitigationState.Unknown;
    }

    private static void ReadProtection(nint handle, ProcessInfo row)
    {
        var status = NativeMethods.NtQueryProcessProtection(
            handle,
            NativeMethods.ProcessProtectionInformationClass,
            out NativeMethods.ProcessProtectionInfo info,
            Marshal.SizeOf<NativeMethods.ProcessProtectionInfo>(),
            out _);
        if (status != 0)
            return;
        if (info.Level == 0 && info.Signer == 0)
        {
            row.Protection = new ProcessProtection("None", null);
            return;
        }
        var level = info.Level switch { 1 => "Light", 2 => "Full", _ => "Level" + info.Level };
        row.Protection = new ProcessProtection(level, info.Signer.ToString());
    }

    private static void ReadDpi(nint handle, ProcessInfo row, List<FieldAvailability> missing)
    {
        try
        {
            var hr = NativeMethods.GetProcessDpiAwareness(handle, out var value);
            if (hr != 0)
            {
                missing.Add(new FieldAvailability(ProcessField.DpiAwareness, Availability.Unsupported, "GetProcessDpiAwareness"));
                return;
            }
            row.DpiAwareness = value switch
            {
                0 => DpiAwareness.Unaware,
                1 => DpiAwareness.System,
                2 => DpiAwareness.PerMonitor,
                3 => DpiAwareness.PerMonitorV2,
                4 => DpiAwareness.UnawareGdiScaled,
                _ => DpiAwareness.Unaware
            };
        }
        catch (DllNotFoundException)
        {
            missing.Add(new FieldAvailability(ProcessField.DpiAwareness, Availability.Unsupported, "shcore"));
        }
    }
}
