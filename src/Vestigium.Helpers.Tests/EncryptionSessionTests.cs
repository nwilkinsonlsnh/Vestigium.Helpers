using System.Security.Cryptography;
using System.Text;
using Vestigium.Helpers.Encryption;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class EncryptionSessionTests
{
    public EncryptionSessionTests()
    {
        HelperLog.Shutdown();
    }

    [Fact]
    public void Identity_is_stable()
        => Assert.Equal("Vestigium.Helpers.Encryption", EncryptionHelper.Identity);

    [Fact]
    public void Probe_is_in_memory_and_logs_pending_then_success()
    {
        var dir = TempDir();
        HelperLog.InitializeHost(HelperLog.AppIds.Encryption, cfg => cfg.LogDirectory = dir);
        try
        {
            var identity = EncryptionHelper.Probe();
            Assert.Equal("Vestigium.Helpers.Encryption", identity);
            var lines = HelperLog.RecentJsonLines;
            Assert.Contains(lines, l => l.Contains("\"STATUS\":\"Pending\""));
            Assert.Contains(lines, l => l.Contains("\"STATUS\":\"Success\""));
            Assert.DoesNotContain(lines, l => l.Contains("gallery-demo-only"));
            Assert.DoesNotContain(lines, l => l.Contains("\"probe\"") && l.Contains("plaintext"));
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }

    [Fact]
    public void Secret_does_not_leak_and_rejects_bad_inputs()
    {
        using var pass = EncryptionSecret.FromPassphrase("gallery-demo-only");
        Assert.Equal("EncryptionSecret", pass.ToString());
        Assert.True(pass.IsPassphrase);
        Assert.Equal(".argon", EncryptionHelper.FileExtension(pass));
        using var key = EncryptionSecret.FromKey(Key());
        Assert.False(key.IsPassphrase);
        Assert.Equal(".aes", EncryptionHelper.FileExtension(key));
        Assert.Equal("EncryptionSecret", key.ToString());
        Assert.Throws<ArgumentException>(() => EncryptionSecret.FromKey(new byte[16]));
        Assert.Throws<ArgumentException>(() => EncryptionSecret.FromPassphrase(" "));
    }

    [Fact]
    public void Validate_without_secret_does_not_reveal_name()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        var dir = TempDir();
        var src = Path.Combine(dir, "nathan.txt");
        File.WriteAllText(src, "hello");
        var sealedPath = EncryptionHelper.SealFile(src, dir, secret);
        var check = EncryptionHelper.ValidateFile(sealedPath);
        Assert.True(check.IsVestigium);
        Assert.True(check.HeaderTrailerAgree);
        Assert.Null(check.StructuralMacValid);
        Assert.True(check.HasHiddenOriginalName);
        Assert.Null(check.OriginalFileName);
        Assert.Equal("1.0", check.Info!.SuiteVersion);
        Assert.False(check.Info.Sha256ReservedFilled);
        Assert.False(check.Info.HmacSha256ReservedFilled);
        Assert.Equal(482, new FileInfo(sealedPath).Length - ("hello".Length + 16 + HeaderLenRawKey()));
    }

    private static int HeaderLenRawKey() => 13 + 4 + 5 + 12 + 4 + 8;

    [Fact]
    public void String_round_trips_utf8()
    {
        using var secret = EncryptionSecret.FromKey(new byte[32]);
        var sealedText = EncryptionHelper.SealString("café Δ", secret);
        Assert.Equal("café Δ", EncryptionHelper.OpenString(sealedText, secret));
    }

    [Fact]
    public void Empty_string_and_empty_file_round_trip()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        Assert.Equal("", EncryptionHelper.OpenString(EncryptionHelper.SealString("", secret), secret));

        var dir = TempDir();
        var src = Path.Combine(dir, "empty.bin");
        File.WriteAllBytes(src, []);
        var sealedPath = EncryptionHelper.SealFile(src, dir, secret);
        Assert.EndsWith(".aes", sealedPath);
        var opened = EncryptionHelper.OpenFile(sealedPath, Path.Combine(dir, "out"), secret);
        Assert.Empty(File.ReadAllBytes(opened));
    }

    [Fact]
    public void Wrong_passphrase_throws_cryptographic_exception()
    {
        using var a = EncryptionSecret.FromPassphrase("correct horse");
        using var b = EncryptionSecret.FromPassphrase("wrong battery");
        var sealedText = EncryptionHelper.SealString("secret", a);
        Assert.Throws<CryptographicException>(() => EncryptionHelper.OpenString(sealedText, b));
    }

    [Fact]
    public void Tampered_blob_throws_cryptographic_exception()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        var blob = Convert.FromBase64String(EncryptionHelper.SealString("hello", secret));
        blob[^20] ^= 0xFF;
        Assert.Throws<CryptographicException>(() =>
            EncryptionHelper.OpenString(Convert.ToBase64String(blob), secret));
    }

    [Fact]
    public void ChaCha_string_round_trip()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        var sealedText = EncryptionHelper.SealString("cha", secret, EncryptionAlgorithm.ChaCha20Poly1305);
        Assert.Equal("cha", EncryptionHelper.OpenString(sealedText, secret));
        var info = PeekBlob(sealedText);
        Assert.Equal(EncryptionAlgorithm.ChaCha20Poly1305, info.Algorithm);
    }

    [Fact]
    public void Raw_key_has_no_salt_in_envelope()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        var blob = Convert.FromBase64String(EncryptionHelper.SealString("x", secret));
        Assert.True(blob.AsSpan(0, 13).SequenceEqual("VESTIGIUM HDR"u8));
        Assert.Equal(0, blob[13 + 4 + 1]); // kdf at header
    }

    [Fact]
    public void Multi_frame_file_round_trips()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        var dir = TempDir();
        var src = Path.Combine(dir, "capture.bin");
        var data = RandomNumberGenerator.GetBytes(200 * 1024);
        File.WriteAllBytes(src, data);
        var sealedPath = EncryptionHelper.SealFile(src, dir, secret);
        var opened = EncryptionHelper.OpenFile(sealedPath, Path.Combine(dir, "restored"), secret);
        Assert.True(SHA256.HashData(data).AsSpan().SequenceEqual(SHA256.HashData(File.ReadAllBytes(opened))));
        var peek = EncryptionHelper.PeekFile(sealedPath);
        Assert.Equal(4UL, peek.FrameCount);
        Assert.Equal((ulong)data.Length, peek.PlaintextLength);
        Assert.True(peek.HasHiddenOriginalName);
        Assert.Null(peek.OriginalFileName);
        Assert.Equal("capture.bin", EncryptionHelper.RevealOriginalFileName(sealedPath, secret));
    }

    [Fact]
    public void Nathan_txt_becomes_aes_and_restores_original_name()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        var dir = TempDir();
        var src = Path.Combine(dir, "nathan.txt");
        File.WriteAllText(src, "hello");
        var sealedPath = EncryptionHelper.SealFile(src, dir, secret);
        Assert.Equal("nathan.aes", Path.GetFileName(sealedPath));
        Assert.True(EncryptionHelper.IsVestigiumFile(sealedPath));
        Assert.True(EncryptionHelper.TryPeekFile(sealedPath, out var peek));
        Assert.Null(peek.OriginalFileName);
        Assert.True(peek.HasHiddenOriginalName);
        var restoredDir = Path.Combine(dir, "out");
        Directory.CreateDirectory(restoredDir);
        var opened = EncryptionHelper.OpenFile(sealedPath, restoredDir, secret);
        Assert.Equal("nathan.txt", Path.GetFileName(opened));
        Assert.Equal("hello", File.ReadAllText(opened));
    }

    [Fact]
    public void Passphrase_file_uses_argon_extension()
    {
        using var secret = EncryptionSecret.FromPassphrase("gallery-demo-only");
        Assert.Equal(".argon", EncryptionHelper.FileExtension(secret));
        Assert.Equal("nathan.argon", EncryptionHelper.SealedFileName("nathan.txt", secret));
        var dir = TempDir();
        var src = Path.Combine(dir, "nathan.txt");
        File.WriteAllText(src, "hello");
        var sealedPath = EncryptionHelper.SealFile(src, dir, secret);
        Assert.Equal("nathan.argon", Path.GetFileName(sealedPath));
        var opened = EncryptionHelper.OpenFile(sealedPath, Path.Combine(dir, "out") + Path.DirectorySeparatorChar, secret);
        Assert.Equal("nathan.txt", Path.GetFileName(opened));
    }

    [Fact]
    public void Missing_source_throws_file_not_found()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        Assert.Throws<FileNotFoundException>(() =>
            EncryptionHelper.SealFile(Path.Combine(TempDir(), "nope.txt"), TempDir(), secret));
    }

    [Fact]
    public void Magics_and_validate()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        var blob = Convert.FromBase64String(EncryptionHelper.SealString("hi", secret));
        Assert.True(blob.AsSpan(0, 13).SequenceEqual("VESTIGIUM HDR"u8));
        Assert.True(blob.AsSpan(blob.Length - 13).SequenceEqual("VESTIGIUM TRL"u8));

        using var ms = new MemoryStream(blob);
        Assert.True(EncryptionHelper.IsVestigium(ms));
        var check = EncryptionHelper.Validate(ms, secret);
        Assert.True(check.IsVestigium);
        Assert.True(check.HeaderPresent);
        Assert.True(check.TrailerPresent);
        Assert.True(check.HeaderTrailerAgree);
        Assert.True(check.StructuralMacValid);
        Assert.Empty(check.Problems);
        Assert.False(check.Info!.Sha256ReservedFilled);
        Assert.False(check.Info.HmacSha256ReservedFilled);
    }

    [Fact]
    public void Truncated_tail_is_not_vestigium_open()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        var blob = Convert.FromBase64String(EncryptionHelper.SealString("hi", secret));
        var clipped = blob[..^17];
        using var ms = new MemoryStream(clipped);
        Assert.True(EncryptionHelper.IsVestigium(ms)); // header still present
        Assert.Throws<CryptographicException>(() =>
        {
            using var dst = new MemoryStream();
            EncryptionHelper.OpenFile(ms, dst, secret);
        });
    }

    [Fact]
    public void Flipped_mac_fails_validate_and_open()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        var blob = Convert.FromBase64String(EncryptionHelper.SealString("hi", secret));
        blob[blob.Length - 17 - 1] ^= 0x01;
        using var ms = new MemoryStream(blob);
        var check = EncryptionHelper.Validate(ms, secret);
        Assert.False(check.StructuralMacValid);
        ms.Position = 0;
        Assert.Throws<CryptographicException>(() =>
        {
            using var dst = new MemoryStream();
            EncryptionHelper.OpenFile(ms, dst, secret);
        });
    }

    [Fact]
    public void Renamed_bin_still_opens()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        var dir = TempDir();
        var src = Path.Combine(dir, "notes.txt");
        File.WriteAllText(src, "keep me");
        var sealedPath = EncryptionHelper.SealFile(src, dir, secret, EncryptionAlgorithm.ChaCha20Poly1305);
        var renamed = Path.Combine(dir, "other.bin");
        File.Move(sealedPath, renamed);
        var outDir = Path.Combine(dir, "out") + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(Path.Combine(dir, "out"));
        var opened = EncryptionHelper.OpenFile(renamed, outDir, secret);
        Assert.Equal("notes.txt", Path.GetFileName(opened));
        Assert.Equal("keep me", File.ReadAllText(opened));
    }

    [Fact]
    public void Random_file_is_not_vestigium()
    {
        var path = Path.Combine(TempDir(), "nope.bin");
        File.WriteAllText(path, "xyz");
        Assert.False(EncryptionHelper.IsVestigiumFile(path));
        Assert.False(EncryptionHelper.TryPeekFile(path, out _));
    }

    [Fact]
    public void NewExportPath_uses_short_suffix()
    {
        using var raw = EncryptionSecret.FromKey(Key());
        using var pass = EncryptionSecret.FromPassphrase("gallery-demo-only");
        Assert.EndsWith(".aes", EncryptionHelper.NewExportPath("Encryption", "nathan.txt", raw));
        Assert.EndsWith("nathan.aes", EncryptionHelper.NewExportPath("Encryption", "nathan.txt", raw));
        Assert.EndsWith("nathan.argon", EncryptionHelper.NewExportPath("Encryption", "nathan.txt", pass));
    }

    [Fact]
    public void Seal_keeps_plaintext_by_default()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        var dir = TempDir();
        var src = Path.Combine(dir, "nathan.txt");
        File.WriteAllText(src, "hello from Vestigium");
        EncryptionHelper.SealFile(src, dir, secret);
        Assert.True(File.Exists(src));
        Assert.Equal("hello from Vestigium", File.ReadAllText(src));
    }

    [Fact]
    public void Seal_with_three_pass_shred_deletes_plaintext()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        var dir = TempDir();
        var src = Path.Combine(dir, "nathan.txt");
        File.WriteAllText(src, "hello from Vestigium");
        var sealedPath = EncryptionHelper.SealFile(src, dir, secret, EncryptionAlgorithm.Aes256Gcm, SecureDeleteMode.ThreePass);
        Assert.False(File.Exists(src));
        Assert.True(File.Exists(sealedPath));
        var opened = EncryptionHelper.OpenFile(sealedPath, Path.Combine(dir, "out") + Path.DirectorySeparatorChar, secret);
        Assert.Equal("nathan.txt", Path.GetFileName(opened));
        Assert.Equal("hello from Vestigium", File.ReadAllText(opened));
    }

    [Fact]
    public void Seven_pass_shred_deletes_empty_file()
    {
        var dir = TempDir();
        var src = Path.Combine(dir, "empty.bin");
        File.WriteAllBytes(src, []);
        EncryptionHelper.SecureDelete(src, SecureDeleteMode.SevenPass);
        Assert.False(File.Exists(src));
    }

    [Fact]
    public void SecureDelete_missing_file_throws()
    {
        Assert.Throws<FileNotFoundException>(() =>
            EncryptionHelper.SecureDelete(Path.Combine(TempDir(), "nope.txt"), SecureDeleteMode.ThreePass));
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncryptionHelper.SecureDelete(Path.Combine(TempDir(), "x"), SecureDeleteMode.Keep));
    }

    private static EncryptionFileInfo PeekBlob(string sealedBase64)
    {
        using var ms = new MemoryStream(Convert.FromBase64String(sealedBase64));
        return EncryptionHelper.Peek(ms);
    }

    private static byte[] Key()
    {
        var key = new byte[32];
        key[0] = 7;
        key[31] = 9;
        return key;
    }

    private static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumEncTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
