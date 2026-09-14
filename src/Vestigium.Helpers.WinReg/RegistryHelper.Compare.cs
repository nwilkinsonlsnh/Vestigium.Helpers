namespace Vestigium.Helpers.WinReg;

public static partial class RegistryHelper
{
    public const int MaxIndexValues = RegistryIndexWriter.MaxValues;
    public const int MaxIndexDepth = RegistryIndexWriter.MaxDepth;

    public static RegistryWriteResult WriteIndex(
        string path,
        RegistryHiveKind hive,
        string? key,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false,
        IProgress<RegistryCompareProgress>? progress = null,
        CancellationToken cancel = default)
        => Local.WriteIndex(path, hive, key, view, confirm, progress, cancel);

    public static RegistryWriteResult Compare(
        string leftIndex,
        string rightIndex,
        string output,
        bool confirm = false,
        bool force = false,
        bool includeSame = false,
        bool includePayload = false,
        IProgress<RegistryCompareProgress>? progress = null,
        CancellationToken cancel = default)
        => RegistryComparer.Compare(leftIndex, rightIndex, output, confirm, force, includeSame, includePayload, progress, cancel);
}
