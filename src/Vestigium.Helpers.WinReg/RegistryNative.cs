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

    [StructLayout(LayoutKind.Sequential)]
    internal struct FileTime
    {
        public uint Low;
        public uint High;
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

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    internal static extern int RegQueryInfoKey(
        SafeRegistryHandle key,
        nint className,
        nint classLen,
        nint reserved,
        nint subKeys,
        nint maxSubKey,
        nint maxClass,
        nint values,
        nint maxValueName,
        nint maxValue,
        nint security,
        out FileTime lastWrite);

    internal static DateTimeOffset? TryLastWrite(SafeRegistryHandle handle)
    {
        if (handle.IsInvalid)
            return null;
        var status = RegQueryInfoKey(handle, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, out var ft);
        if (status != 0)
            return null;
        var ticks = ((long)ft.High << 32) | ft.Low;
        if (ticks <= 0)
            return null;
        try { return DateTimeOffset.FromFileTime(ticks); }
        catch { return null; }
    }

    internal static PrivilegeScope BackupRestore() => new("SeBackupPrivilege", "SeRestorePrivilege");

    internal static bool EnablePrivileges(params string[] names)
    {
        using var scope = new PrivilegeScope(names);
        return scope.Enabled;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool CloseHandle(nint handle);

    internal sealed class PrivilegeScope : IDisposable
    {
        private readonly List<Luid> _luids = [];
        public bool Enabled { get; }

        public PrivilegeScope(params string[] names)
        {
            if (!OpenProcessToken(GetCurrentProcess(), TokenAdjustPrivileges | TokenQuery, out var token))
            {
                Enabled = false;
                return;
            }

            var ok = true;
            try
            {
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
                    else
                        _luids.Add(luid);
                }
            }
            finally
            {
                CloseHandle(token);
            }

            Enabled = ok && _luids.Count > 0;
        }

        public void Dispose()
        {
            if (_luids.Count == 0)
                return;
            if (!OpenProcessToken(GetCurrentProcess(), TokenAdjustPrivileges | TokenQuery, out var token))
                return;
            try
            {
                foreach (var luid in _luids)
                {
                    var priv = new TokenPrivileges { PrivilegeCount = 1, Luid = luid, Attributes = 0 };
                    AdjustTokenPrivileges(token, false, ref priv, 0, 0, 0);
                }
            }
            finally
            {
                CloseHandle(token);
            }
        }
    }
}
