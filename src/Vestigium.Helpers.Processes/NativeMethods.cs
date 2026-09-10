using System.Runtime.InteropServices;
using System.Text;

namespace Vestigium.Helpers.Processes;

internal static class NativeMethods
{
    internal const uint ProcessQueryLimitedInformation = 0x1000;
    internal const uint ProcessVmRead = 0x0010;
    internal const int ProcessBasicInformationClass = 0;
    internal const int ProcessProtectionInformationClass = 61;
    internal const uint TokenQuery = 0x0008;
    internal const int TokenIntegrityLevel = 25;
    internal const int TokenUiAccess = 26;
    internal const int TokenVirtualizationEnabled = 24;

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern nint OpenProcess(uint desiredAccess, bool inheritHandle, int processId);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool CloseHandle(nint handle);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool GetProcessIoCounters(nint handle, out IoCounters counters);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool QueryFullProcessImageName(
        nint handle,
        int flags,
        StringBuilder exeName,
        ref int size);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool IsWow64Process(nint handle, out bool wow64);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool ReadProcessMemory(
        nint process,
        nint baseAddress,
        byte[] buffer,
        int size,
        out nint read);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern bool GetProcessMitigationPolicy(
        nint handle,
        int policy,
        ref uint flags,
        int length);

    [DllImport("ntdll.dll")]
    internal static extern int NtQueryInformationProcess(
        nint processHandle,
        int processInformationClass,
        out ProcessBasicInfo processInformation,
        int processInformationLength,
        out int returnLength);

    [DllImport("ntdll.dll")]
    internal static extern int NtQueryInformationProcess(
        nint processHandle,
        int processInformationClass,
        out ProcessProtectionInfo processInformation,
        int processInformationLength,
        out int returnLength);

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern bool OpenProcessToken(nint processHandle, uint desiredAccess, out nint tokenHandle);

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern bool GetTokenInformation(
        nint tokenHandle,
        int tokenInformationClass,
        nint tokenInformation,
        int tokenInformationLength,
        out int returnLength);

    [DllImport("user32.dll")]
    internal static extern bool IsWindowVisible(nint hwnd);

    [DllImport("user32.dll")]
    internal static extern bool IsIconic(nint hwnd);

    [DllImport("user32.dll")]
    internal static extern bool IsZoomed(nint hwnd);

    [DllImport("user32.dll")]
    internal static extern bool IsHungAppWindow(nint hwnd);

    [DllImport("shcore.dll")]
    internal static extern int GetProcessDpiAwareness(nint handle, out int awareness);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern int GetPackageFamilyName(nint handle, ref int length, StringBuilder? name);

    [DllImport("wintrust.dll", ExactSpelling = true)]
    internal static extern uint WinVerifyTrust(nint hwnd, ref Guid actionId, nint data);

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProcessBasicInfo
    {
        public nint Reserved1;
        public nint PebBaseAddress;
        public nint Reserved2_0;
        public nint Reserved2_1;
        public nint UniqueProcessId;
        public nint InheritedFromUniqueProcessId;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProcessProtectionInfo
    {
        public byte Level;
        public byte Signer;
        public ushort Reserved;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct IoCounters
    {
        public ulong ReadOperationCount;
        public ulong WriteOperationCount;
        public ulong OtherOperationCount;
        public ulong ReadTransferCount;
        public ulong WriteTransferCount;
        public ulong OtherTransferCount;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WinTrustFileInfo
    {
        public uint Size;
        public nint FilePath;
        public nint FileHandle;
        public nint KnownSubject;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct WinTrustData
    {
        public uint Size;
        public nint PolicyCallbackData;
        public nint SIPClientData;
        public uint UIChoice;
        public uint RevocationChecks;
        public uint UnionChoice;
        public nint FileInfo;
        public uint StateAction;
        public nint StateData;
        public nint URLReference;
        public uint ProvFlags;
        public uint UIContext;
    }
}
