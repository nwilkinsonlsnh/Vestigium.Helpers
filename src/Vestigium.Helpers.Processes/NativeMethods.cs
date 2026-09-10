using System.Runtime.InteropServices;
using System.Text;

namespace Vestigium.Helpers.Processes;

internal static class NativeMethods
{
    internal const uint ProcessQueryLimitedInformation = 0x1000;
    internal const int ProcessBasicInformation = 0;

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

    [DllImport("ntdll.dll")]
    internal static extern int NtQueryInformationProcess(
        nint processHandle,
        int processInformationClass,
        out ProcessBasicInformation processInformation,
        int processInformationLength,
        out int returnLength);

    [StructLayout(LayoutKind.Sequential)]
    internal struct ProcessBasicInformation
    {
        public nint Reserved1;
        public nint PebBaseAddress;
        public nint Reserved2_0;
        public nint Reserved2_1;
        public nint UniqueProcessId;
        public nint InheritedFromUniqueProcessId;
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
}
