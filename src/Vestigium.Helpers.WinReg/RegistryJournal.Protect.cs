using System.Security.Cryptography;
using System.Text;

namespace Vestigium.Helpers.WinReg;

public sealed partial class RegistryJournal
{
    internal string Seal(string? plain)
    {
        if (plain is null || !Protect)
            return plain ?? "";
        var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(plain), null, DataProtectionScope.CurrentUser);
        return Convert.ToBase64String(bytes);
    }

    internal static string? Open(string? sealedPayload, bool protect)
    {
        if (sealedPayload is null)
            return null;
        if (!protect)
            return sealedPayload;
        try
        {
            var bytes = ProtectedData.Unprotect(Convert.FromBase64String(sealedPayload), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    public static RegistryWriteResult Purge(string path, bool confirm)
    {
        path = HelperGuard.NotBlank(path, nameof(path));
        if (!confirm)
            return new RegistryWriteResult(RegistryWriteStatus.Denied, RegistryHiveKind.CurrentUser, path, null, "confirm=false");
        if (IsOpen(path))
            return new RegistryWriteResult(RegistryWriteStatus.InUse, RegistryHiveKind.CurrentUser, path, null, "journal open");
        if (!File.Exists(path))
            return new RegistryWriteResult(RegistryWriteStatus.NotFound, RegistryHiveKind.CurrentUser, path, null, "journal missing");
        File.Delete(path);
        return new RegistryWriteResult(RegistryWriteStatus.Ok, RegistryHiveKind.CurrentUser, path, null, null);
    }
}
