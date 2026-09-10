using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Processes;

internal static class ProcessStarter
{
    private const uint LogonWithProfile = 1;
    private const uint CreateNoWindowFlag = 0x08000000;

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool CreateProcessWithLogonW(
        string userName,
        string? domain,
        string password,
        uint logonFlags,
        string? applicationName,
        string? commandLine,
        uint creationFlags,
        nint environment,
        string? currentDirectory,
        ref StartupInfo startupInfo,
        out ProcessInformation processInformation);

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct StartupInfo
    {
        public int Size;
        public string? Reserved;
        public string? Desktop;
        public string? Title;
        public int X, Y, XSize, YSize, XCountChars, YCountChars, FillAttribute, Flags;
        public short ShowWindow, Reserved2;
        public nint Reserved3, StdInput, StdOutput, StdError;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct ProcessInformation
    {
        public nint Process;
        public nint Thread;
        public int ProcessId;
        public int ThreadId;
    }

    internal static ProcessStartResult Start(ProcessStartRequest request)
    {
        var file = HelperGuard.NotBlank(request.FileName, nameof(request.FileName)).Trim();
        try
        {
            var info = new ProcessStartInfo
            {
                FileName = file,
                Arguments = request.Arguments ?? "",
                UseShellExecute = request.UseShellExecute,
                CreateNoWindow = request.CreateNoWindow
            };
            if (!string.IsNullOrWhiteSpace(request.WorkingDirectory))
                info.WorkingDirectory = request.WorkingDirectory;
            if (!string.IsNullOrWhiteSpace(request.Verb))
                info.Verb = request.Verb;
            if (request.RedirectStandardIO)
            {
                info.RedirectStandardInput = true;
                info.RedirectStandardOutput = true;
                info.RedirectStandardError = true;
                info.UseShellExecute = false;
            }
            if (request.Environment is not null)
            {
                foreach (var pair in request.Environment)
                    info.Environment[pair.Key] = pair.Value;
            }

            var process = Process.Start(info);
            if (process is null)
                return ProcessStartResult.Fail(ProcessStartError.Unknown, "Process.Start returned null.");
            var pid = process.Id;
            process.Dispose();
            LogStart(file, pid, null, true);
            return ProcessStartResult.Success(pid);
        }
        catch (Exception ex)
        {
            var error = MapStart(ex);
            LogStart(file, null, null, false, error);
            return ProcessStartResult.Fail(error, ex.Message);
        }
    }

    internal static ProcessStartResult StartAs(ProcessStartRequest request, ProcessStartAs credentials)
    {
        var file = HelperGuard.NotBlank(request.FileName, nameof(request.FileName)).Trim();
        var user = HelperGuard.NotBlank(credentials.UserName, nameof(credentials.UserName)).Trim();
        if (credentials.Password is null || credentials.Password.Length == 0)
            return ProcessStartResult.Fail(ProcessStartError.LogonFailed, "Password is required.");

        var raw = Marshal.SecureStringToGlobalAllocUnicode(credentials.Password);
        var password = Marshal.PtrToStringUni(raw) ?? "";
        try
        {
            var command = string.IsNullOrWhiteSpace(request.Arguments) ? file : "\"" + file + "\" " + request.Arguments;
            var flags = (uint)credentials.LogonFlags;
            if (credentials.LoadUserProfile || flags == 0)
                flags |= LogonWithProfile;

            var startup = new StartupInfo { Size = Marshal.SizeOf<StartupInfo>() };
            var created = CreateProcessWithLogonW(
                user,
                string.IsNullOrWhiteSpace(credentials.Domain) ? null : credentials.Domain,
                password,
                flags,
                file,
                command,
                request.CreateNoWindow ? CreateNoWindowFlag : 0,
                nint.Zero,
                string.IsNullOrWhiteSpace(request.WorkingDirectory) ? null : request.WorkingDirectory,
                ref startup,
                out var info);

            if (!created)
            {
                var code = Marshal.GetLastWin32Error();
                var error = code is 1326 or 1331 or 1385 or 1314 ? ProcessStartError.LogonFailed
                    : code is 2 or 3 ? ProcessStartError.FileNotFound
                    : code == 5 ? ProcessStartError.AccessDenied
                    : ProcessStartError.Unknown;
                LogStart(file, null, user, false, error);
                return ProcessStartResult.Fail(error, "Win32 " + code);
            }

            if (info.Process != nint.Zero) NativeMethods.CloseHandle(info.Process);
            if (info.Thread != nint.Zero) NativeMethods.CloseHandle(info.Thread);
            LogStart(file, info.ProcessId, user, true);
            return ProcessStartResult.Success(info.ProcessId);
        }
        catch (Exception ex)
        {
            var error = MapStart(ex);
            LogStart(file, null, user, false, error);
            return ProcessStartResult.Fail(error, ex.Message);
        }
        finally
        {
            Marshal.ZeroFreeGlobalAllocUnicode(raw);
        }
    }

    private static ProcessStartError MapStart(Exception ex) => ex switch
    {
        FileNotFoundException => ProcessStartError.FileNotFound,
        DirectoryNotFoundException => ProcessStartError.FileNotFound,
        BadImageFormatException => ProcessStartError.InvalidImage,
        Win32Exception win when win.NativeErrorCode is 2 or 3 => ProcessStartError.FileNotFound,
        Win32Exception win when win.NativeErrorCode == 5 => ProcessStartError.AccessDenied,
        Win32Exception win when win.NativeErrorCode is 1326 or 1331 or 1385 => ProcessStartError.LogonFailed,
        Win32Exception win when win.NativeErrorCode == 1223 => ProcessStartError.Cancelled,
        UnauthorizedAccessException => ProcessStartError.AccessDenied,
        _ => ProcessStartError.Unknown
    };

    private static void LogStart(string file, int? pid, string? user, bool ok, ProcessStartError? error = null)
    {
        var line = "Start file=" + Path.GetFileName(file);
        if (user is not null) line += " user=" + user;
        if (pid is int value) line += " pid=" + value;
        if (error is { } err) line += " error=" + err;
        HelperLog.Information(
            HelperLog.AppIds.Processes,
            ok ? VestigiumStatus.Success : VestigiumStatus.Failed,
            HelperLog.Subcategories.Inventory,
            line);
    }
}
