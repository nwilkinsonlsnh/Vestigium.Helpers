using System.Security.Cryptography;
using Vestigium.Helpers.Hashing;

namespace Vestigium.Helpers.Pdf;

internal static class PdfSeal
{
    private static string Path => System.IO.Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
        "Vestigium",
        "Pdf",
        "seal.key");

    public static string Body(byte[] pdf)
        => HashingHelper.HashBytes(pdf, HashingAlgorithm.Sha256);

    public static string Seal(PdfIdentity identity, string bodyHash)
        => Seal(identity.Canonical(), bodyHash);

    public static string Seal(string canonical, string bodyHash)
    {
        using var key = Open();
        return HashingHelper.HmacString(canonical + "\nbody=" + bodyHash, key, HmacAlgorithm.Sha256);
    }

    private static HmacKey Open()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("The seal key is held with DPAPI.");
        var path = Path;
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        if (!File.Exists(path))
        {
            var raw = RandomNumberGenerator.GetBytes(32);
            File.WriteAllBytes(path, ProtectedData.Protect(raw, null, DataProtectionScope.LocalMachine));
            return HmacKey.FromBytes(raw);
        }
        var key = ProtectedData.Unprotect(File.ReadAllBytes(path), null, DataProtectionScope.LocalMachine);
        return HmacKey.FromBytes(key);
    }
}
