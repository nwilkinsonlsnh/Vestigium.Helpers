namespace Vestigium.Helpers.WinReg;

public sealed partial class RegistryJournal
{
    public static bool IsOpen(string path)
    {
        path = System.IO.Path.GetFullPath(path);
        if (!File.Exists(path))
            return false;
        try
        {
            using var _ = new FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None);
            return false;
        }
        catch (IOException)
        {
            return true;
        }
    }
}
