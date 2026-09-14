using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace Vestigium.Helpers.WinReg;

internal static partial class RegistryAcl
{
    private const uint WriteDac = 0x00040000;
    private const uint WriteOwner = 0x00080000;
    private const uint ReadControl = 0x00020000;
    private const uint KeyRead = 0x20019;

    public static uint TrySetOwner(SafeRegistryHandle handle, SecurityIdentifier sid)
    {
        var buffer = new byte[sid.BinaryLength];
        sid.GetBinaryForm(buffer, 0);
        var ptr = Marshal.AllocHGlobal(buffer.Length);
        try
        {
            Marshal.Copy(buffer, 0, ptr, buffer.Length);
            using var scope = new RegistryNative.PrivilegeScope("SeTakeOwnershipPrivilege", "SeRestorePrivilege");
            return SetSecurityInfo(handle, SeRegistryKey, OwnerInformation, ptr, 0, 0, 0);
        }
        finally
        {
            Marshal.FreeHGlobal(ptr);
        }
    }

    public static uint TrySetSddl(SafeRegistryHandle handle, string sddl)
    {
        if (!sddl.Contains("D:", StringComparison.OrdinalIgnoreCase))
            return 87; // ERROR_INVALID_PARAMETER
        if (!ConvertStringSecurityDescriptorToSecurityDescriptor(sddl, SddlRevision1, out var sd, out _))
            return (uint)Marshal.GetLastPInvokeError();
        try
        {
            var status = GetSecurityInfo(handle, SeRegistryKey, DaclInformation, out _, out _, out var dacl, out _, out var copy);
            if (copy != 0) LocalFree(copy);
            _ = dacl;
            if (!ConvertStringSecurityDescriptorToSecurityDescriptor(sddl, SddlRevision1, out var parsed, out _))
                return (uint)Marshal.GetLastPInvokeError();
            try
            {
                status = GetSecurityInfo(IntPtr.Zero, SeRegistryKey, 0, out _, out _, out _, out _, out _);
                _ = status;
            }
            finally
            {
                LocalFree(parsed);
            }

            var daclPtr = GetDacl(sd);
            using var scope = new RegistryNative.PrivilegeScope("SeSecurityPrivilege", "SeRestorePrivilege");
            return SetSecurityInfo(handle, SeRegistryKey, DaclInformation, 0, 0, daclPtr, 0);
        }
        finally
        {
            LocalFree(sd);
        }
    }

    private static nint GetDacl(nint sd)
    {
        if (!GetSecurityDescriptorDacl(sd, out var present, out var dacl, out _) || !present)
            return 0;
        return dacl;
    }

    public static bool TryResolveAccount(string account, out SecurityIdentifier sid, out string? error)
    {
        sid = new SecurityIdentifier(WellKnownSidType.WorldSid, null);
        error = null;
        try
        {
            if (account.StartsWith("S-", StringComparison.OrdinalIgnoreCase))
            {
                sid = new SecurityIdentifier(account);
                return true;
            }

            sid = (SecurityIdentifier)new NTAccount(account).Translate(typeof(SecurityIdentifier));
            return true;
        }
        catch (Exception ex)
        {
            error = ex.GetType().Name;
            return false;
        }
    }

    public static SecurityIdentifier CurrentUser()
        => WindowsIdentity.GetCurrent().User ?? new SecurityIdentifier(WellKnownSidType.WorldSid, null);

    public static SafeRegistryHandle? OpenWriteAcl(RegistryHiveKind hive, string path, out uint status)
    {
        status = 0;
        var root = HiveHandle(hive);
        if (root == 0)
        {
            status = 87;
            return null;
        }

        var access = KeyRead | ReadControl | WriteDac | WriteOwner;
        status = (uint)RegOpenKeyEx(root, string.IsNullOrEmpty(path) ? null : path, 0, access, out var handle);
        return status == 0 ? handle : null;
    }

    private static nint HiveHandle(RegistryHiveKind hive) => hive switch
    {
        RegistryHiveKind.ClassesRoot => unchecked((nint)(int)0x80000000),
        RegistryHiveKind.CurrentUser => unchecked((nint)(int)0x80000001),
        RegistryHiveKind.LocalMachine => unchecked((nint)(int)0x80000002),
        RegistryHiveKind.Users => unchecked((nint)(int)0x80000003),
        RegistryHiveKind.CurrentConfig => unchecked((nint)(int)0x80000005),
        _ => 0
    };

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern uint SetSecurityInfo(
        SafeRegistryHandle handle,
        int objectType,
        uint securityInfo,
        nint owner,
        nint group,
        nint dacl,
        nint sacl);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern uint SetSecurityInfo(
        nint handle,
        int objectType,
        uint securityInfo,
        nint owner,
        nint group,
        nint dacl,
        nint sacl);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool ConvertStringSecurityDescriptorToSecurityDescriptor(
        string stringSecurityDescriptor,
        uint revision,
        out nint descriptor,
        out uint size);

    [DllImport("advapi32.dll", SetLastError = true)]
    private static extern bool GetSecurityDescriptorDacl(nint descriptor, out bool present, out nint dacl, out bool defaulted);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern int RegOpenKeyEx(nint key, string? subKey, int options, uint access, out SafeRegistryHandle result);
}
