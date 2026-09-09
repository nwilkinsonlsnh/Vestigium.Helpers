using System.Security.Cryptography;
using System.Text;
using Vestigium.Helpers;
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

    [Fact]
    public void Cbc_string_round_trip_peeks_suite_1_1()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        var sealedText = EncryptionHelper.SealString("cbc interop", secret, EncryptionAlgorithm.Aes256CbcHmac);
        Assert.Equal("cbc interop", EncryptionHelper.OpenString(sealedText, secret));
        var info = PeekBlob(sealedText);
        Assert.Equal(EncryptionAlgorithm.Aes256CbcHmac, info.Algorithm);
        Assert.Equal("1.1", info.SuiteVersion);
        Assert.Equal("1.0", info.TrailerVersion);
        Assert.Equal("", EncryptionHelper.OpenString(EncryptionHelper.SealString("", secret, EncryptionAlgorithm.Aes256CbcHmac), secret));
    }

    [Fact]
    public void Cbc_pkcs7_sizes_and_hidden_name()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        foreach (var n in new[] { 1, 15, 16, 17 })
        {
            var text = new string('x', n);
            Assert.Equal(text, EncryptionHelper.OpenString(EncryptionHelper.SealString(text, secret, EncryptionAlgorithm.Aes256CbcHmac), secret));
        }

        var dir = TempDir();
        var src = Path.Combine(dir, "nathan.txt");
        File.WriteAllText(src, "hello");
        var sealedPath = EncryptionHelper.SealFile(src, dir, secret, EncryptionAlgorithm.Aes256CbcHmac);
        Assert.Equal("nathan.aes", Path.GetFileName(sealedPath));
        Assert.True(EncryptionHelper.TryPeekFile(sealedPath, out var peek));
        Assert.Equal(EncryptionAlgorithm.Aes256CbcHmac, peek.Algorithm);
        Assert.Equal("1.1", peek.SuiteVersion);
        Assert.True(peek.HasHiddenOriginalName);
        Assert.Null(peek.OriginalFileName);
        Assert.Equal("nathan.txt", EncryptionHelper.RevealOriginalFileName(sealedPath, secret));
        var restoredDir = Path.Combine(dir, "out") + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(Path.Combine(dir, "out"));
        var opened = EncryptionHelper.OpenFile(sealedPath, restoredDir, secret);
        Assert.Equal("nathan.txt", Path.GetFileName(opened));
        Assert.Equal("hello", File.ReadAllText(opened));
    }

    [Fact]
    public void Cbc_flipped_frame_hmac_and_iv_fail_closed()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        var blob = Convert.FromBase64String(EncryptionHelper.SealString("keep", secret, EncryptionAlgorithm.Aes256CbcHmac));
        var hmacFlip = blob.ToArray();
        hmacFlip[^483] ^= 0xFF;
        using (var ms = new MemoryStream(hmacFlip))
        {
            var check = EncryptionHelper.Validate(ms, secret);
            Assert.True(check.StructuralMacValid);
            ms.Position = 0;
            Assert.Throws<CryptographicException>(() =>
            {
                using var dst = new MemoryStream();
                EncryptionHelper.OpenFile(ms, dst, secret);
            });
        }

        var ivFlip = blob.ToArray();
        ivFlip[HeaderLenRawKey()] ^= 0xFF;
        using (var ms = new MemoryStream(ivFlip))
        {
            Assert.Throws<CryptographicException>(() =>
            {
                using var dst = new MemoryStream();
                EncryptionHelper.OpenFile(ms, dst, secret);
            });
        }
    }

    [Fact]
    public void Cbc_multi_frame_file_round_trips()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        var dir = TempDir();
        var src = Path.Combine(dir, "capture.bin");
        var data = RandomNumberGenerator.GetBytes(200 * 1024);
        File.WriteAllBytes(src, data);
        var sealedPath = EncryptionHelper.SealFile(src, dir, secret, EncryptionAlgorithm.Aes256CbcHmac);
        var opened = EncryptionHelper.OpenFile(sealedPath, Path.Combine(dir, "restored"), secret);
        Assert.True(SHA256.HashData(data).AsSpan().SequenceEqual(SHA256.HashData(File.ReadAllBytes(opened))));
        var peek = EncryptionHelper.PeekFile(sealedPath);
        Assert.Equal(EncryptionAlgorithm.Aes256CbcHmac, peek.Algorithm);
        Assert.Equal("1.1", peek.SuiteVersion);
        Assert.Equal(4UL, peek.FrameCount);
        Assert.Equal((ulong)data.Length, peek.PlaintextLength);
        Assert.Equal("capture.bin", EncryptionHelper.RevealOriginalFileName(sealedPath, secret));
    }

    [Fact]
    public void Rsa_wrap_round_trips_and_peeks_suite_1_2()
    {
        using var appX = EncryptionRsaKey.Generate(2048);
        var sealedText = EncryptionHelper.SealString("token from Vestigium", [appX]);
        Assert.Equal("token from Vestigium", EncryptionHelper.OpenString(sealedText, appX));
        var info = PeekBlob(sealedText);
        Assert.Equal("1.2", info.SuiteVersion);
        Assert.Equal("1.0", info.TrailerVersion);
        Assert.Equal(EncryptionAlgorithm.Aes256Gcm, info.Algorithm);
        Assert.True(info.HasRsaWrap);
        Assert.Equal(1, info.WrapCount);
        Assert.Equal(appX.ThumbprintHex, info.WrapThumbprints[0]);
        Assert.False(info.UsedArgon2id);
    }

    [Fact]
    public void Rsa_wrap_isolates_appx_from_appy()
    {
        using var appX = EncryptionRsaKey.Generate(2048);
        using var appY = EncryptionRsaKey.Generate(2048);
        var sealedText = EncryptionHelper.SealString("only AppX", [appX]);
        Assert.Equal("only AppX", EncryptionHelper.OpenString(sealedText, appX));
        var ex = Assert.Throws<CryptographicException>(() => EncryptionHelper.OpenString(sealedText, appY));
        Assert.Equal("The envelope is corrupt.", ex.Message);
        var blob = Convert.FromBase64String(sealedText);
        var ascii = Encoding.ASCII.GetString(blob);
        Assert.DoesNotContain("CompanyX", ascii);
        Assert.DoesNotContain("ApplicationX", ascii);
        Assert.DoesNotContain("AppX wrap", ascii);
    }

    [Fact]
    public void Rsa_also_wrap_to_me_opens_with_either_key()
    {
        using var ops = EncryptionRsaKey.Generate(2048);
        using var appX = EncryptionRsaKey.Generate(2048);
        var sealedText = EncryptionHelper.SealString("shared", [appX], alsoWrapTo: ops);
        Assert.Equal("shared", EncryptionHelper.OpenString(sealedText, appX));
        Assert.Equal("shared", EncryptionHelper.OpenString(sealedText, ops));
        var info = PeekBlob(sealedText);
        Assert.Equal(2, info.WrapCount);
        Assert.Contains(appX.ThumbprintHex, info.WrapThumbprints);
        Assert.Contains(ops.ThumbprintHex, info.WrapThumbprints);
    }

    [Fact]
    public void Rsa_wrap_hides_nathan_and_round_trips_file()
    {
        using var appX = EncryptionRsaKey.Generate(2048);
        var dir = TempDir();
        var src = Path.Combine(dir, "nathan.txt");
        File.WriteAllText(src, "hello");
        var sealedPath = EncryptionHelper.SealFile(src, dir, [appX]);
        Assert.EndsWith(".aes", sealedPath);
        Assert.Equal("nathan.aes", Path.GetFileName(sealedPath));
        var peek = EncryptionHelper.PeekFile(sealedPath);
        Assert.Equal("1.2", peek.SuiteVersion);
        Assert.True(peek.HasHiddenOriginalName);
        Assert.Null(peek.OriginalFileName);
        Assert.DoesNotContain("nathan.txt", File.ReadAllText(sealedPath));
        Assert.Equal("nathan.txt", EncryptionHelper.RevealOriginalFileName(sealedPath, appX));
        var restoredDir = Path.Combine(dir, "out") + Path.DirectorySeparatorChar;
        Directory.CreateDirectory(Path.Combine(dir, "out"));
        var opened = EncryptionHelper.OpenFile(sealedPath, restoredDir, appX);
        Assert.Equal("nathan.txt", Path.GetFileName(opened));
        Assert.Equal("hello", File.ReadAllText(opened));
    }

    [Fact]
    public void Rsa_wrap_200kib_file_round_trips()
    {
        using var appX = EncryptionRsaKey.Generate(2048);
        var dir = TempDir();
        var src = Path.Combine(dir, "capture.bin");
        var data = RandomNumberGenerator.GetBytes(200 * 1024);
        File.WriteAllBytes(src, data);
        var sealedPath = EncryptionHelper.SealFile(src, dir, [appX]);
        var opened = EncryptionHelper.OpenFile(sealedPath, Path.Combine(dir, "restored"), appX);
        Assert.True(SHA256.HashData(data).AsSpan().SequenceEqual(SHA256.HashData(File.ReadAllBytes(opened))));
        var peek = EncryptionHelper.PeekFile(sealedPath);
        Assert.Equal("1.2", peek.SuiteVersion);
        Assert.Equal(4UL, peek.FrameCount);
        Assert.Equal("capture.bin", EncryptionHelper.RevealOriginalFileName(sealedPath, appX));
    }

    [Fact]
    public void Rsa_secret_plus_wrap_opens_with_either()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        using var appX = EncryptionRsaKey.Generate(2048);
        var sealedText = EncryptionHelper.SealString("both", secret, EncryptionAlgorithm.Aes256Gcm, [appX]);
        Assert.Equal("both", EncryptionHelper.OpenString(sealedText, secret));
        Assert.Equal("both", EncryptionHelper.OpenString(sealedText, appX));
        Assert.Equal("1.2", PeekBlob(sealedText).SuiteVersion);
    }

    [Fact]
    public void Rsa_cbc_wrap_is_suite_1_2_with_alg_3()
    {
        using var appX = EncryptionRsaKey.Generate(2048);
        var sealedText = EncryptionHelper.SealString("cbc wrap", [appX], EncryptionAlgorithm.Aes256CbcHmac);
        Assert.Equal("cbc wrap", EncryptionHelper.OpenString(sealedText, appX));
        var info = PeekBlob(sealedText);
        Assert.Equal("1.2", info.SuiteVersion);
        Assert.Equal(EncryptionAlgorithm.Aes256CbcHmac, info.Algorithm);
        Assert.Equal(1, info.WrapCount);
    }

    [Fact]
    public void Key_ring_issue_is_public_only_unless_escrowed()
    {
        using var ring = EncryptionKeyRing.Create("Ops ring");
        var (contact, slip) = ring.Issue("Company X AppX", "AppX wrap", "CompanyX", application: "ApplicationX", keyBits: 2048);
        Assert.False(contact.Key.CanUnwrap);
        Assert.Null(ring.FindPrivate(contact.Key.Thumbprint));
        Assert.True(slip.CanUnwrap);
        var sealedText = EncryptionHelper.SealString("slip", [contact.Key]);
        Assert.Equal("slip", EncryptionHelper.OpenString(sealedText, slip));
        Assert.Throws<CryptographicException>(() => EncryptionHelper.OpenString(sealedText, ring));
        slip.Dispose();

        var (escrowed, kept) = ring.Issue("Company X AppY", "AppY wrap", "CompanyX", application: "ApplicationY", keyBits: 2048, escrow: true);
        Assert.NotNull(ring.FindPrivate(escrowed.Key.Thumbprint));
        var sealedY = EncryptionHelper.SealString("escrow", [escrowed.Key]);
        Assert.Equal("escrow", EncryptionHelper.OpenString(sealedY, ring));
        kept.Dispose();
    }

    [Fact]
    public void Key_ring_json_round_trips_and_rejects_over_limit()
    {
        using var ring = EncryptionKeyRing.Create("Ops ring");
        ring.AddPair("Ops receive", "Ops", "Wilkinson", application: "Gallery", keyBits: 2048);
        using var pub = EncryptionRsaKey.Generate(2048);
        ring.AddContact("Company X AppX", "AppX wrap", "CompanyX", pub, application: "ApplicationX");
        var json = ring.ToJson();
        Assert.Contains("VESTIGIUM-KEYRING", json);
        using var back = EncryptionKeyRing.FromJson(json);
        Assert.Single(back.Pairs);
        Assert.Single(back.Contacts);
        Assert.True(back.Pairs[0].Key.CanUnwrap);
        Assert.False(back.Contacts[0].Key.CanUnwrap);
        Assert.Equal("ApplicationX", back.Contacts[0].Application);
        Assert.Equal(ring.Pairs[0].ThumbprintSha256, back.Pairs[0].ThumbprintSha256);

        Assert.Throws<ArgumentException>(() => EncryptionKeyRing.Create(new string('T', 76)));
        Assert.Throws<ArgumentException>(() => ring.AddContact(new string('T', 76), "s", "c", pub));
        Assert.Throws<ArgumentException>(() => ring.AddContact("t", new string('S', 51), "c", pub));
        Assert.Throws<ArgumentException>(() => ring.AddContact("t", "s", new string('C', 76), pub));
        Assert.Throws<ArgumentException>(() => ring.AddContact("t", "s", "c", pub, application: new string('A', 51)));
        Assert.Throws<ArgumentException>(() => ring.AddContact("t", "s", "c", pub, description: new string('D', 221)));
    }

    [Fact]
    public void Token_disable_requires_override_and_never_logs_key_material()
    {
        var dir = TempDir();
        HelperLog.InitializeHost(HelperLog.AppIds.Encryption, cfg => cfg.LogDirectory = dir);
        try
        {
            using var ring = EncryptionKeyRing.Create("Ops ring");
            var pair = ring.AddPair("Ops receive", "Ops", "CompanyX", application: "ApplicationX", keyBits: 2048);
            var sealedText = EncryptionHelper.SealString("token", [pair.Key]);
            Assert.Equal("token", EncryptionHelper.OpenString(sealedText, ring));

            ring.Disable(pair);
            var disabled = Assert.Throws<EncryptionTokenException>(() => EncryptionHelper.OpenString(sealedText, ring));
            Assert.Equal("The token is disabled.", disabled.Message);
            Assert.Equal(EncryptionKeyStatus.Disabled, disabled.Status);

            var ov = EncryptionKeyOverride.Request("qa-operator", "restore for incident 42");
            Assert.Equal("token", EncryptionHelper.OpenString(sealedText, ring, ov));

            using var slip = EncryptionRsaKey.FromPkcs8(pair.Key.ExportPkcs8());
            Assert.Equal("token", EncryptionHelper.OpenString(sealedText, slip));

            ring.Enable(pair);
            Assert.Equal("token", EncryptionHelper.OpenString(sealedText, ring));

            ring.Expire(pair, DateTimeOffset.UtcNow.AddMinutes(-1));
            var expired = Assert.Throws<EncryptionTokenException>(() => ring.RequireForSeal(pair));
            Assert.Equal("The token is expired.", expired.Message);
            Assert.NotNull(ring.RequireForSeal(pair, EncryptionKeyOverride.Request("qa-operator", "read-only restore")));

            ring.Compromise(pair);
            var terminal = Assert.Throws<EncryptionTokenException>(
                () => ring.RequireForOpen(pair, EncryptionKeyOverride.Request("qa-operator", "attempt after compromise")));
            Assert.Equal("The token is not usable.", terminal.Message);
            Assert.Equal(EncryptionKeyStatus.Compromised, pair.Status);
            Assert.Throws<EncryptionTokenException>(() => ring.Disable(pair));
            Assert.Throws<EncryptionTokenException>(() => ring.Enable(pair));
            Assert.Equal(EncryptionKeyStatus.Compromised, pair.Status);

            var lines = string.Join('\n', HelperLog.RecentJsonLines);
            Assert.Contains("\"SUBCATEGORY\":\"Token\"", lines);
            Assert.Contains("Disable:", lines);
            Assert.Contains("Override:", lines);
            Assert.Contains("by=qa-operator", lines);
            Assert.Contains("reason=restore for incident 42", lines);
            Assert.Contains(pair.ThumbprintSha256[..8], lines);
            Assert.DoesNotContain("CompanyX", lines, StringComparison.Ordinal);
            Assert.DoesNotContain("ApplicationX", lines, StringComparison.Ordinal);
            Assert.DoesNotContain("BEGIN ", lines, StringComparison.Ordinal);
            Assert.DoesNotContain("PRIVATE KEY", lines, StringComparison.Ordinal);
            Assert.DoesNotContain("PKCS8", lines, StringComparison.Ordinal);
            Assert.DoesNotContain(pair.ThumbprintSha256, lines, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(sealedText, lines, StringComparison.Ordinal);
            Assert.All(HelperLog.RecentJsonLines, line => Assert.DoesNotContain("\"EXCEPTION\":\"", line.Replace("\"EXCEPTION\":null", "")));
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }

    [Fact]
    public void Token_override_rejects_key_material_and_retired_is_not_overridable()
    {
        Assert.Throws<ArgumentException>(() => EncryptionKeyOverride.Request("qa", "-----BEGIN PRIVATE KEY-----"));
        Assert.Throws<ArgumentException>(() => EncryptionKeyOverride.Request("qa", new string('a', 44)));
        Assert.Throws<ArgumentException>(() => EncryptionKeyOverride.Request("qa", new string('f', 32)));
        var ok = EncryptionKeyOverride.Request("qa-operator", "restore disabled token");
        Assert.Equal("qa-operator", ok.RequestedBy);

        using var ring = EncryptionKeyRing.Create("Ops ring");
        var pair = ring.AddPair("Ops receive", "Ops", "Wilkinson", application: "Gallery", keyBits: 2048);
        ring.Retire(pair);
        var ov = EncryptionKeyOverride.Request("qa-operator", "restore retired token");
        var retired = Assert.Throws<EncryptionTokenException>(() => ring.RequireForOpen(pair, ov));
        Assert.Equal("The token is not usable.", retired.Message);
        Assert.Throws<EncryptionTokenException>(() => ring.Enable(pair));

        ring.Enable(ring.AddPair("Ops two", "Ops", "Wilkinson", application: "Other", keyBits: 2048), DateTimeOffset.UtcNow.AddSeconds(-1));
        var clock = ring.Pairs[^1];
        Assert.Equal(EncryptionKeyStatus.Expired, ring.EffectiveStatus(clock));
        Assert.NotNull(ring.RequireForSeal(clock, EncryptionKeyOverride.Request("qa-operator", "clock skew restore")));

        var json = ring.ToJson();
        Assert.Contains("\"formatMinor\": 1", json);
        using var back = EncryptionKeyRing.FromJson(json);
        Assert.Equal(EncryptionKeyStatus.Retired, back.Pairs[0].Status);
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
