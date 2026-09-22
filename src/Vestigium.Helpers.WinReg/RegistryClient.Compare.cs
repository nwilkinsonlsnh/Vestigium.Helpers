namespace Vestigium.Helpers.WinReg;

public sealed partial class RegistryClient
{
    public RegistryWriteResult WriteIndex(
        string path,
        RegistryHiveKind hive,
        string? key,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false,
        bool includePayload = false,
        IProgress<RegistryCompareProgress>? progress = null,
        CancellationToken cancel = default)
        => RegistryIndexWriter.Write(this, path, hive, key, view, confirm, includePayload, progress, cancel);
}
