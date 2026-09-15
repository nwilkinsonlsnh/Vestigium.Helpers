namespace Vestigium.Helpers.WinReg;

public static partial class RegistryHelper
{
    public static RegistryJournal? CreateJournal(string path, bool confirm, out RegistryWriteResult result, bool protect = false)
        => RegistryJournal.Create(path, confirm, out result, protect);

    public static RegistryJournal? LoadJournal(string path, bool confirm, out RegistryWriteResult result)
        => RegistryJournal.Load(path, confirm, out result);

    public static RegistryJournalInfo ReadJournal(string path)
        => RegistryJournal.ReadInfo(path);

    public static RegistryWriteResult PurgeJournal(string path, bool confirm = false)
        => RegistryJournal.Purge(path, confirm);

    public static RegistryPurgeResult PurgeJournal(string path, RegistryPurgeOptions options)
        => RegistryJournal.Compact(path, options);

    public static RegistryWriteResult Import(
        string path,
        RegistryJournal journal,
        RegistryViewKind view = RegistryViewKind.Default,
        bool confirm = false,
        IProgress<RegistryCompareProgress>? progress = null,
        CancellationToken cancel = default,
        IReadOnlyList<RegistryHiveKind>? allowedHives = null)
        => Local.Import(path, view, confirm, progress, cancel, allowedHives, journal);

    public static RegistryWriteResult Restore(
        string snapshotPath,
        bool confirm,
        string? journalPath = null,
        bool protect = false,
        IProgress<RegistryCompareProgress>? progress = null,
        CancellationToken cancel = default)
    {
        RegistryJournal? journal = null;
        if (journalPath is not null)
        {
            journal = CreateJournal(journalPath, confirm, out var created, protect);
            if (journal is null)
                return created;
        }

        try
        {
            var result = RegistryRestore.Run(Local, snapshotPath, confirm, journal, progress, cancel);
            journal?.CommitBatch();
            return result;
        }
        finally
        {
            journal?.Dispose();
        }
    }

    public static RegistryWriteResult Rollback(string journalPath, bool confirm = false, bool force = false)
        => RegistryJournal.Rollback(journalPath, Local, confirm, force);
}
