namespace Vestigium.Helpers.WinReg;

public static partial class RegistryHelper
{
    public static RegistryJournal? CreateJournal(string path, bool confirm, bool protect = false, out RegistryWriteResult result)
        => RegistryJournal.Create(path, confirm, protect, out result);

    public static RegistryJournal? LoadJournal(string path, bool confirm, out RegistryWriteResult result)
        => RegistryJournal.Load(path, confirm, out result);

    public static RegistryJournalInfo ReadJournal(string path)
        => RegistryJournal.ReadInfo(path);
}
