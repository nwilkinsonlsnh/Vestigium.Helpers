using System.Runtime.InteropServices;
using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.Services;

internal static class ServiceLogon
{
    public const string LocalSystemName = "LocalSystem";
    public const string LocalServiceName = @"NT AUTHORITY\LocalService";
    public const string NetworkServiceName = @"NT AUTHORITY\NetworkService";
    public const string SeServiceLogonRight = "SeServiceLogonRight";
    public const string SeBatchLogonRight = "SeBatchLogonRight";

    public static ServiceControlResult Set(string name, ServiceLogonRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (!request.Confirm)
            return new ServiceControlResult(name, ServiceControlStatus.Denied, null, "confirm=false");
        if (request.InteractWithDesktop && request.Kind != ServiceLogonKind.LocalSystem)
            return new ServiceControlResult(name, ServiceControlStatus.InvalidState, null, "InteractWithDesktop requires LocalSystem");

        var (account, password) = request.Kind switch
        {
            ServiceLogonKind.LocalService => (LocalServiceName, string.Empty),
            ServiceLogonKind.NetworkService => (NetworkServiceName, string.Empty),
            ServiceLogonKind.Account => (HelperGuard.NotBlank(request.Account, nameof(request.Account)), request.Password ?? string.Empty),
            _ => (LocalSystemName, string.Empty)
        };

        if (request.Kind == ServiceLogonKind.Account && request.GrantLogonRight != ServiceGrantLogonRight.None)
        {
            var grant = Grant(account, request.GrantLogonRight);
            if (grant.Reason is not null && !grant.HasServiceLogon && request.GrantLogonRight.HasFlag(ServiceGrantLogonRight.Service))
                return new ServiceControlResult(name, ServiceControlStatus.Denied, null, grant.Reason);
        }

        var handle = ServiceSnapshotter.Open(name, ServiceNative.ServiceChangeConfig | ServiceNative.ServiceQueryConfig);
        if (handle == 0)
            return new ServiceControlResult(name, ServiceControlStatus.Denied, null, "OpenService");
        try
        {
            uint serviceType = ServiceNative.ServiceNoChange;
            if (request.Kind == ServiceLogonKind.LocalSystem)
            {
                serviceType = (uint)ServiceTypeFlags.Win32OwnProcess;
                if (request.InteractWithDesktop)
                    serviceType |= (uint)ServiceTypeFlags.InteractiveProcess;
            }
            if (!ServiceNative.ChangeServiceConfig(handle, serviceType, ServiceNative.ServiceNoChange, ServiceNative.ServiceNoChange, null, null, 0, null, account, password, null))
                return new ServiceControlResult(name, ServiceControlStatus.Denied, null, "ChangeServiceConfig");
            HelperLog.Information(HelperLog.AppIds.Services, VestigiumStatus.Success, HelperLog.Subcategories.Start, $"SetLogon {name} kind={request.Kind} account={account}");
            var snap = ServiceSnapshotter.CaptureName(name, ServiceDetailLevel.Slim, joinProcess: false);
            return new ServiceControlResult(name, ServiceControlStatus.Ok, snap?.Status, null);
        }
        finally { ServiceNative.CloseServiceHandle(handle); }
    }

    public static ServiceAccountRightInfo QueryRights(string account)
    {
        var name = HelperGuard.NotBlank(account, nameof(account));
        if (!TrySid(name, out var sid, out var reason))
            return new ServiceAccountRightInfo(name, false, false, reason);
        var attrs = new ServiceNative.LsaObjectAttributes { Length = Marshal.SizeOf<ServiceNative.LsaObjectAttributes>() };
        var status = ServiceNative.LsaOpenPolicy(0, ref attrs, ServiceNative.PolicyLookupNames, out var policy);
        if (status != 0)
            return new ServiceAccountRightInfo(name, false, false, "LsaOpenPolicy " + Win(status));
        try
        {
            status = ServiceNative.LsaEnumerateAccountRights(policy, sid, out var buffer, out var count);
            if (status != 0)
                return new ServiceAccountRightInfo(name, false, false, status == unchecked((int)0xC0000034) ? null : "LsaEnumerateAccountRights " + Win(status));
            try
            {
                var hasService = false;
                var hasBatch = false;
                var stride = Marshal.SizeOf<ServiceNative.LsaUnicodeString>();
                for (var i = 0; i < count; i++)
                {
                    var row = Marshal.PtrToStructure<ServiceNative.LsaUnicodeString>(buffer + (i * stride));
                    var right = Marshal.PtrToStringUni(row.Buffer, row.Length / 2);
                    if (string.Equals(right, SeServiceLogonRight, StringComparison.OrdinalIgnoreCase)) hasService = true;
                    if (string.Equals(right, SeBatchLogonRight, StringComparison.OrdinalIgnoreCase)) hasBatch = true;
                }
                return new ServiceAccountRightInfo(name, hasService, hasBatch, null);
            }
            finally { ServiceNative.LsaFreeMemory(buffer); }
        }
        finally { ServiceNative.LsaClose(policy); }
    }

    public static ServiceAccountRightInfo Grant(string account, ServiceGrantLogonRight rights)
    {
        var name = HelperGuard.NotBlank(account, nameof(account));
        if (rights == ServiceGrantLogonRight.None)
            return QueryRights(name);
        if (!TrySid(name, out var sid, out var reason))
            return new ServiceAccountRightInfo(name, false, false, reason);
        var wanted = new List<string>();
        if (rights.HasFlag(ServiceGrantLogonRight.Service)) wanted.Add(SeServiceLogonRight);
        if (rights.HasFlag(ServiceGrantLogonRight.Batch)) wanted.Add(SeBatchLogonRight);
        var handles = new List<nint>();
        var strings = new ServiceNative.LsaUnicodeString[wanted.Count];
        for (var i = 0; i < wanted.Count; i++)
        {
            var ptr = Marshal.StringToHGlobalUni(wanted[i]);
            handles.Add(ptr);
            strings[i] = new ServiceNative.LsaUnicodeString { Length = (ushort)(wanted[i].Length * 2), MaximumLength = (ushort)((wanted[i].Length + 1) * 2), Buffer = ptr };
        }
        var attrs = new ServiceNative.LsaObjectAttributes { Length = Marshal.SizeOf<ServiceNative.LsaObjectAttributes>() };
        var status = ServiceNative.LsaOpenPolicy(0, ref attrs, ServiceNative.PolicyLookupNames | ServiceNative.PolicyCreateAccount | 0x0002, out var policy);
        try
        {
            if (status != 0)
                return new ServiceAccountRightInfo(name, false, false, "LsaOpenPolicy " + Win(status));
            status = ServiceNative.LsaAddAccountRights(policy, sid, strings, strings.Length);
            if (status != 0)
                return new ServiceAccountRightInfo(name, false, false, "LsaAddAccountRights " + Win(status));
            HelperLog.Information(HelperLog.AppIds.Services, VestigiumStatus.Success, HelperLog.Subcategories.Start, $"GrantLogonRight account={name} rights={rights}");
            return QueryRights(name);
        }
        finally
        {
            if (policy != 0) ServiceNative.LsaClose(policy);
            foreach (var handle in handles) Marshal.FreeHGlobal(handle);
        }
    }

    private static bool TrySid(string account, out byte[] sid, out string? reason)
    {
        sid = new byte[256];
        var sidLen = sid.Length;
        var domain = new char[256];
        var domainLen = domain.Length;
        if (!ServiceNative.LookupAccountName(null, account, sid, ref sidLen, domain, ref domainLen, out _))
        {
            sid = [];
            reason = "LookupAccountName " + Marshal.GetLastWin32Error();
            return false;
        }
        if (sidLen != sid.Length) Array.Resize(ref sid, sidLen);
        reason = null;
        return true;
    }

    private static string Win(int ntStatus) => ServiceNative.LsaNtStatusToWinError(ntStatus).ToString();
}
