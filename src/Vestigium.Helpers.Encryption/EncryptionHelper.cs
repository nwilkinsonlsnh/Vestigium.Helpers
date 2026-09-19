using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;
using Vestigium.Logging;

namespace Vestigium.Helpers.Encryption;

/// <summary>
/// Authenticated encryption helpers (AES-256-GCM default, ChaCha20-Poly1305, AES-256-CBC+HMAC v1.1, RSA-OAEP wrap v1.2, Argon2id).
/// Libraries never call Initialize. Hashing lives in Vestigium.Helpers.Hashing. RSA never encrypts payload frames.
/// </summary>
public static class EncryptionHelper
{
    public static string Identity => "Vestigium.Helpers.Encryption";

    public static string Probe()
    {
        EncryptionLog.Pending("Probe", "Opening an encryption helper probe.");
        using var secret = EncryptionSecret.FromPassphrase("gallery-demo-only");
        var sealedText = SealString("probe", secret);
        var back = OpenString(sealedText, secret);
        if (back != "probe")
            throw new CryptographicException("The envelope is corrupt.");
        EncryptionLog.Success("Probe", "Encryption probe complete. Identity=" + Identity);
        return Identity;
    }

    public static string DefaultExportDirectory(string appId)
    {
        var id = EncryptionLog.RequireNotBlank(appId, nameof(appId));
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        if (string.IsNullOrWhiteSpace(desktop))
            desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
        if (string.IsNullOrWhiteSpace(desktop))
        {
            var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            desktop = Path.Combine(string.IsNullOrWhiteSpace(home) ? "." : home, "Desktop");
        }

        return Path.Combine(desktop, "Vestigium", "Exports", id);
    }

    public static string FileExtension(EncryptionSecret secret)
    {
        ArgumentNullException.ThrowIfNull(secret);
        return secret.IsPassphrase ? ".argon" : ".aes";
    }

    public static string SealedFileName(string originalFileName, EncryptionSecret secret)
        => OriginalNames.Stem(originalFileName) + FileExtension(secret);

    public static string NewExportPath(string appId, string? originalFileName = null, EncryptionSecret? secret = null)
    {
        var id = EncryptionLog.RequireNotBlank(appId, nameof(appId));
        string file;
        if (!string.IsNullOrWhiteSpace(originalFileName) && secret is not null)
            file = SealedFileName(originalFileName, secret);
        else
        {
            var stamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
            var ext = secret is null ? ".aes" : FileExtension(secret);
            file = $"vestigium-{id}-{stamp}{ext}";
        }

        foreach (var ch in Path.GetInvalidFileNameChars())
            file = file.Replace(ch, '_');
        return Path.Combine(DefaultExportDirectory(id), file);
    }
