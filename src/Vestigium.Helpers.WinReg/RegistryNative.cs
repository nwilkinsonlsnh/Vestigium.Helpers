using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Vestigium.Helpers.WinReg;

internal static class RegistryNative
{
    internal const uint TokenAdjustPrivileges = 0x0020;
    internal const uint TokenQuery = 0x0008;
    internal const uint SePrivilegeEnabled = 0x00000002;
    internal const uint RegStandardFormat = 1;

    [StructLayout(LayoutKind.Sequential)]
    internal struct Luid
    {
        public uint LowPart;
        public int HighPart;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct TokenPrivileges
    {
        public int PrivilegeCount;
        public Luid Luid;
        public uint Attributes;
    }

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern bool LookupPrivilegeValue(string? system, string name, out Luid luid);

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern bool OpenProcessToken(nint process, uint access, out nint token);

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern bool AdjustTokenPrivileges(nint token, bool disableAll, ref TokenPrivileges NewState, int bufferLength, nint previous, nint required);

    [DllImport("kernel32.dll")]
    internal static extern nint GetCurrentProcess();

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern int RegSaveKeyEx(SafeRegistryHandle key, string file, nint security, uint flags);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern int RegLoadKey(nint hive, string subKey, string file);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    internal static extern int RegUnLoadKey(nint hive, string subKey);

    internal static bool EnablePrivileges(params string[] names)
    {
        if (!OpenProcessToken(GetCurrentProcess(), TokenAdjustPrivileges | TokenQuery, out var token))
            return false;
        try
        {
            var ok = true;
            foreach (var name in names)
            {
                if (!LookupPrivilegeValue(null, name, out var luid))
                {
                    ok = false;
                    continue;
                }

                var priv = new TokenPrivileges { PrivilegeCount = 1, Luid = luid, Attributes = SePrivilegeEnabled };
                if (!AdjustTokenPrivileges(token, false, ref priv, 0, 0, 0))
                    ok = false;
            }
            return ok;
        }
        finally
        {
            Marshal.FreeHGlobal(token == 0 ? 0 : 0);
            CloseHandle(token);
        }
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(nint handle);
}
