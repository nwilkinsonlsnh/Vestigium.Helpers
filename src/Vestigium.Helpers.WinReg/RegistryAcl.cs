using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace Vestigium.Helpers.WinReg;

internal static partial class RegistryAcl
{
    internal const int SeRegistryKey = 6;
    internal const uint OwnerInformation = 0x00000001;
    internal const uint DaclInformation = 0x00000004;
    internal const uint SddlRevision1 = 1;

    public static void TryRead(SafeRegistryHandle handle, out string? owner, out string? sddl, out string? error)
    {
        owner = null;
        sddl = null;
        error = null;
        if (handle.IsInvalid)
        {
            error = "invalid handle";
            return;
        }

        var status = GetSecurityInfo(
            handle,
            SeRegistryKey,
            OwnerInformation | DaclInformation,
            out var ownerSid,
            out _,
            out _,
            out _,
            out var sd);
        if (status != 0 || sd == 0)
        {
            error = "GetSecurityInfo=" + status;
            return;
        }

        try
        {
            owner = TranslateOwner(ownerSid) ?? SidString(ownerSid);
            if (ConvertSecurityDescriptorToStringSecurityDescriptor(sd, SddlRevision1, OwnerInformation | DaclInformation, out var sddlPtr, out _))
            {
                try { sddl = Marshal.PtrToStringUni(sddlPtr); }
                finally { if (sddlPtr != 0) LocalFree(sddlPtr); }
            }
        }
        finally
        {
            LocalFree(sd);
        }
    }

    private static string? TranslateOwner(nint sid)
    {
        if (sid == 0)
            return null;
        try
        {
            return new SecurityIdentifier(sid).Translate(typeof(NTAccount)).Value;
        }
        catch
        {
            return SidString(sid);
        }
    }

    private static string? SidString(nint sid)
    {
        if (sid == 0 || !ConvertSidToStringSid(sid, out var ptr))
            return null;
        try { return Marshal.PtrToStringUni(ptr); }
        finally { if (ptr != 0) LocalFree(ptr); }
    }

    [DllImport("advapi32.dll", SetLastError = true)]
    internal static extern uint GetSecurityInfo(
        SafeRegistryHandle handle,
        int objectType,
        uint securityInfo,
        out nint owner,
        out nint group,
        out nint dacl,
        out nint sacl,
        out nint securityDescriptor);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool ConvertSecurityDescriptorToStringSecurityDescriptor(
        nint descriptor,
        uint revision,
        uint securityInfo,
        out nint stringSecurityDescriptor,
        out uint stringLength);

    [DllImport("advapi32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool ConvertSidToStringSid(nint sid, out nint stringSid);

    [DllImport("kernel32.dll")]
    internal static extern nint LocalFree(nint block);
}
