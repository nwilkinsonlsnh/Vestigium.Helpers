namespace Vestigium.Helpers.WinReg;

public sealed partial class RegistryJournal
{
    public static string ReadText(string path)
        => string.Join(Environment.NewLine, ReadSharedLines(HelperGuard.FileExists(path, nameof(path))));

    public RegistryWriteResult RecordRename(
        RegistryHiveKind hive,
        string path,
        string from,
        string to,
        RegistryValueInfo before)
    {
        _ = EnsureBatch();
        var row = new Dictionary<string, object?>
        {
            ["op"] = "RenameValue",
            ["hive"] = hive.ToString(),
            ["path"] = path,
            ["name"] = from,
            ["to"] = to,
            ["existed"] = true
        };
        PutPayload(row, "before", before);
        return AppendMut(row);
    }
}
