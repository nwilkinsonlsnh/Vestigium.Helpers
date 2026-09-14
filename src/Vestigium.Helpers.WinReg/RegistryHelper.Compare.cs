namespace Vestigium.Helpers.WinReg;

public static partial class RegistryHelper
{
    public const int MaxIndexValues = RegistryIndexWriter.MaxValues;
    public const int MaxIndexDepth = RegistryIndexWriter.MaxDepth;

    public static RegistryWriteResult CopyKey(
        RegistryHiveKind sourceHive,
        string? sourceKey,
        RegistryHiveKind destHive,
        string? destKey,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false,
        IProgress<RegistryCompareProgress>? progress = null,
        CancellationToken cancel = default)
        => Local.CopyKey(sourceHive, sourceKey, destHive, destKey, view, confirm, progress, cancel);

    public static RegistryWriteResult RenameKey(
        RegistryHiveKind hive,
        string? sourceKey,
        string? destKey,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false,
        IProgress<RegistryCompareProgress>? progress = null,
        CancellationToken cancel = default)
        => Local.RenameKey(hive, sourceKey, destKey, view, confirm, progress, cancel);

    public static RegistryWriteResult WriteIndex(
        string path,
        RegistryHiveKind hive,
        string? key,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false,
        bool includePayload = false,
        IProgress<RegistryCompareProgress>? progress = null,
        CancellationToken cancel = default)
        => Local.WriteIndex(path, hive, key, view, confirm, includePayload, progress, cancel);

    public static RegistryWriteResult IndexFromReg(
        string regPath,
        string indexPath,
        bool confirm = false,
        bool includePayload = false,
        IProgress<RegistryCompareProgress>? progress = null,
        CancellationToken cancel = default)
        => RegistryIndexFromReg.Write(regPath, indexPath, confirm, includePayload, progress, cancel);

    public static RegistryWriteResult Compare(
        string leftIndex,
        string rightIndex,
        string output,
        bool confirm = false,
        bool force = false,
        bool includeSame = false,
        bool includePayload = false,
        IProgress<RegistryCompareProgress>? progress = null,
        CancellationToken cancel = default,
        IReadOnlyList<string>? ignorePathPrefixes = null)
    {
        var summary = CompareDetailed(leftIndex, rightIndex, output, confirm, force, includeSame, includePayload, progress, cancel, ignorePathPrefixes);
        return new RegistryWriteResult(summary.Status, RegistryHiveKind.CurrentUser, output, null, summary.Reason ?? summary.Verdict);
    }

    public static RegistryCompareSummary CompareDetailed(
        string leftIndex,
        string rightIndex,
        string output,
        bool confirm = false,
        bool force = false,
        bool includeSame = false,
        bool includePayload = false,
        IProgress<RegistryCompareProgress>? progress = null,
        CancellationToken cancel = default,
        IReadOnlyList<string>? ignorePathPrefixes = null)
        => RegistryComparer.Compare(leftIndex, rightIndex, output, confirm, force, includeSame, includePayload, progress, cancel, ignorePathPrefixes);
}
