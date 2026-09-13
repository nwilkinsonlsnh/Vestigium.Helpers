namespace Vestigium.Helpers.WinReg;

public static partial class RegistryHelper
{
    public static RegistryWriteResult WriteIndex(
        string path,
        RegistryHiveKind hive,
        string? key,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false,
        IProgress<RegistryCompareProgress>? progress = null,
        CancellationToken cancel = default)
        => RegistryIndexWriter.Write(Local, path, hive, key, view, confirm, progress, cancel);
}
