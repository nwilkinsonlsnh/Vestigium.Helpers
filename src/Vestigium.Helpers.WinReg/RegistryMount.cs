using Vestigium.Helpers;
using Vestigium.Logging;

namespace Vestigium.Helpers.WinReg;

public interface IRegistryMount : IDisposable
{
    string HiveFile { get; }
    RegistryHiveKind Destination { get; }
    string SubKey { get; }
    bool IsLoaded { get; }
    RegistryClient Client { get; }
    RegistryWriteResult Dismount(bool confirm = false);
}

internal sealed class RegistryMount : IRegistryMount
{
    private const int HkeyLocalMachineRaw = unchecked((int)0x80000002);
    private const int HkeyUsersRaw = unchecked((int)0x80000003);

    internal static nint HkeyLocalMachine => HkeyLocalMachineRaw;
    internal static nint HkeyUsers => HkeyUsersRaw;

    private int _unloaded;

    internal RegistryMount(string hiveFile, RegistryHiveKind destination, string subKey)
    {
        HiveFile = hiveFile;
        Destination = destination;
        SubKey = subKey;
        IsLoaded = true;
        Client = RegistryHelper.Local;
    }

    public string HiveFile { get; }
    public RegistryHiveKind Destination { get; }
    public string SubKey { get; }
    public bool IsLoaded { get; private set; }
    public RegistryClient Client { get; }

    public RegistryWriteResult Dismount(bool confirm = false)
    {
        if (!IsLoaded || Interlocked.Exchange(ref _unloaded, 1) == 1)
            return new RegistryWriteResult(RegistryWriteStatus.Ok, Destination, SubKey, null, "already dismounted");
        if (!confirm)
        {
            Interlocked.Exchange(ref _unloaded, 0);
            return new RegistryWriteResult(RegistryWriteStatus.Denied, Destination, SubKey, null, "confirm=false");
        }

        _ = RegistryNative.EnablePrivileges("SeBackupPrivilege", "SeRestorePrivilege");
        var status = RegistryNative.RegUnLoadKey(HiveHandle(Destination), SubKey);
        if (status != 0)
        {
            Interlocked.Exchange(ref _unloaded, 0);
            return new RegistryWriteResult(RegistryWriteStatus.Denied, Destination, SubKey, null, "RegUnLoadKey=" + status);
        }

        IsLoaded = false;
        HelperLog.Information(HelperLog.AppIds.WinReg, VestigiumStatus.Success, HelperLog.Subcategories.Inventory, $"Dismount dest={Destination} sub={SubKey}");
        return new RegistryWriteResult(RegistryWriteStatus.Ok, Destination, SubKey, null, null);
    }

    public void Dispose()
    {
        if (IsLoaded)
            _ = Dismount(confirm: true);
    }

    internal static nint HiveHandle(RegistryHiveKind destination) => destination switch
    {
        RegistryHiveKind.LocalMachine => HkeyLocalMachine,
        RegistryHiveKind.Users => HkeyUsers,
        _ => 0
    };
}
