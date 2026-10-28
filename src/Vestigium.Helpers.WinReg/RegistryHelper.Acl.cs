namespace Vestigium.Helpers.WinReg;

public static partial class RegistryHelper
{
    public static RegistryWriteResult SetOwner(
        RegistryHiveKind hive,
        string? key,
        string account,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false)
        => Local.SetOwner(hive, key, account, view, confirm);

    public static RegistryWriteResult TakeOwnership(
        RegistryHiveKind hive,
        string? key,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false)
        => Local.TakeOwnership(hive, key, view, confirm);

    public static RegistryWriteResult SetSddl(
        RegistryHiveKind hive,
        string? key,
        string sddl,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false)
        => Local.SetSddl(hive, key, sddl, view, confirm);
}
