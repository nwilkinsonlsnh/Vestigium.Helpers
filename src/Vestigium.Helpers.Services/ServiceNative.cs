using System.Runtime.InteropServices;

namespace Vestigium.Helpers.Services;

internal static class ServiceNative
{
    public const uint ScManagerConnect = 0x0001;
    public const uint ScManagerEnumerate = 0x0004;
    public const uint ServiceQueryConfig = 0x0001;
    public const uint ServiceChangeConfig = 0x0002;
    public const uint ServiceQueryStatus = 0x0004;
    public const uint ServiceStart = 0x0010;
    public const uint ServiceStop = 0x0020;
    public const uint ServicePauseContinue = 0x0040;
    public const uint ServiceAllAccess = 0xF01FF;
    public const uint ServiceNoChange = 0xFFFFFFFF;
    public const uint ServiceConfigDescription = 1;
    public const uint ServiceConfigFailureActions = 2;
    public const uint ServiceConfigDelayedAutoStart = 3;
    public const uint ServiceConfigFailureActionsFlag = 4;
    public const uint ServiceConfigServiceSidInfo = 5;
    public const uint ServiceConfigRequiredPrivileges = 6;
    public const uint ServiceConfigPreshutdown = 7;
    public const uint ServiceConfigTriggerInfo = 8;
    public const uint ServiceConfigLaunchProtected = 12;
    public const int StatusProcessInfo = 0;

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern nint OpenSCManager(string? machine, string? database, uint access);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern nint OpenService(nint scm, string name, uint access);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool CloseServiceHandle(nint handle);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern bool QueryServiceStatusEx(nint service, int infoLevel, nint buffer, int size, out int needed);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool QueryServiceConfig(nint service, nint buffer, int size, out int needed);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool QueryServiceConfig2(nint service, uint infoLevel, nint buffer, int size, out int needed);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool ChangeServiceConfig(
        nint service,
        uint serviceType,
        uint startType,
        uint errorControl,
        string? binaryPath,
        string? loadOrderGroup,
        nint tagId,
        string? dependencies,
        string? account,
        string? password,
        string? displayName);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool ChangeServiceConfig2(nint service, uint infoLevel, nint info);

    [DllImport("advapi32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    public static extern bool LookupAccountName(
        string? system,
        string account,
        byte[] sid,
        ref int sidLen,
        char[] domain,
        ref int domainLen,
        out int use);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern uint LsaNtStatusToWinError(int status);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern int LsaOpenPolicy(nint system, ref LsaObjectAttributes attrs, int access, out nint handle);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern int LsaClose(nint handle);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern int LsaAddAccountRights(nint policy, byte[] sid, LsaUnicodeString[] rights, int count);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern int LsaEnumerateAccountRights(nint policy, byte[] sid, out nint rights, out int count);

    [DllImport("advapi32.dll", SetLastError = true)]
    public static extern int LsaFreeMemory(nint buffer);

    public const int PolicyCreateAccount = 0x0010;
    public const int PolicyLookupNames = 0x0800;

    [StructLayout(LayoutKind.Sequential)]
    public struct ServiceStatusProcess
    {
        public uint ServiceType;
        public uint CurrentState;
        public uint ControlsAccepted;
        public uint Win32ExitCode;
        public uint ServiceSpecificExitCode;
        public uint CheckPoint;
        public uint WaitHint;
        public uint ProcessId;
        public uint ServiceFlags;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct QueryServiceConfigData
    {
        public uint ServiceType;
        public uint StartType;
        public uint ErrorControl;
        public nint BinaryPathName;
        public nint LoadOrderGroup;
        public uint TagId;
        public nint Dependencies;
        public nint ServiceStartName;
        public nint DisplayName;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct DelayedAutoStartInfo
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool DelayedAutostart;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FailureActions
    {
        public uint ResetPeriod;
        public nint RebootMsg;
        public nint Command;
        public uint Count;
        public nint Actions;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ScAction
    {
        public uint Type;
        public uint Delay;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct LsaObjectAttributes
    {
        public int Length;
        public nint RootDirectory;
        public nint ObjectName;
        public uint Attributes;
        public nint SecurityDescriptor;
        public nint SecurityQualityOfService;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct LsaUnicodeString
    {
        public ushort Length;
        public ushort MaximumLength;
        public nint Buffer;
    }

    public static string? PtrToString(nint ptr)
        => ptr == 0 ? null : Marshal.PtrToStringUni(ptr);
}
