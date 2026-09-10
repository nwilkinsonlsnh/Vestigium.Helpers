using System.Security.Cryptography;
using Vestigium.Helpers.Encryption;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class EncryptionCoverageTests
{
    [Fact]
    public void SealOptions_wants_flags_follow_key_and_coverage()
    {
        var none = new EncryptionSealOptions();
        Assert.Equal(16, EncryptionSealOptions.MinCallerMacKeyLength);
        Assert.False(none.EmbedPlaintextSha256);
        Assert.Null(none.CallerMacKey);
        Assert.Equal(HmacCoverage.None, none.Coverage);
        Assert.False(none.WantsCallerMac);
        Assert.False(none.WantsAny);

        var shaOnly = new EncryptionSealOptions { EmbedPlaintextSha256 = true };
        Assert.True(shaOnly.WantsAny);
        Assert.False(shaOnly.WantsCallerMac);

        var emptyKey = new EncryptionSealOptions
        {
            CallerMacKey = [],
            Coverage = HmacCoverage.Plaintext
        };
        Assert.False(emptyKey.WantsCallerMac);

        var coverageNone = new EncryptionSealOptions
        {
            CallerMacKey = new byte[16],
            Coverage = HmacCoverage.None
        };
        Assert.False(coverageNone.WantsCallerMac);
        Assert.False(coverageNone.WantsAny);

        var caller = new EncryptionSealOptions
        {
            CallerMacKey = new byte[16],
            Coverage = HmacCoverage.CiphertextFrames
        };
        Assert.True(caller.WantsCallerMac);
        Assert.True(caller.WantsAny);

        var header = new EncryptionSealOptions
        {
            CallerMacKey = new byte[32],
            Coverage = HmacCoverage.HeaderAndFrames,
            EmbedPlaintextSha256 = true
        };
        Assert.True(header.WantsCallerMac);
        Assert.True(header.WantsAny);
        Assert.Equal(HmacCoverage.Plaintext, (HmacCoverage)3);
    }

    [Fact]
    public void RsaKey_generate_bounds_and_corrupt_imports()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => EncryptionRsaKey.Generate(2047));
        Assert.Throws<ArgumentOutOfRangeException>(() => EncryptionRsaKey.Generate(4097));
        Assert.Throws<ArgumentOutOfRangeException>(() => EncryptionRsaKey.Generate(2050));
        var corruptSpki = Assert.Throws<CryptographicException>(() => EncryptionRsaKey.FromPublicSpki([1, 2, 3, 4]));
        Assert.Equal("The envelope is corrupt.", corruptSpki.Message);
        var corruptPkcs8 = Assert.Throws<CryptographicException>(() => EncryptionRsaKey.FromPkcs8([9, 8, 7]));
        Assert.Equal("The envelope is corrupt.", corruptPkcs8.Message);
    }

    [Fact]
    public void RsaKey_public_only_cannot_unwrap_or_export_private()
    {
        using var pair = EncryptionRsaKey.Generate(2048);
        Assert.True(pair.CanUnwrap);
        Assert.Equal(2048, pair.KeyBits);
        Assert.Contains("RSA-2048", pair.ToString(), StringComparison.Ordinal);
        Assert.True(pair.ThumbprintEquals(pair.Thumbprint));
        Assert.False(pair.ThumbprintEquals(new byte[16]));
        Assert.False(pair.ThumbprintEquals(new byte[32]));

        using var pub = pair.PublicOnly();
        Assert.False(pub.CanUnwrap);
        Assert.Equal(pair.ThumbprintHex, pub.ThumbprintHex);
        Assert.Throws<InvalidOperationException>(() => pub.ExportPkcs8());
        var wrapped = pair.Wrap(new byte[32]);
        var corrupt = Assert.Throws<CryptographicException>(() => pub.Unwrap(wrapped));
        Assert.Equal("The envelope is corrupt.", corrupt.Message);

        var tooShort = Assert.Throws<ArgumentException>(() => pair.Wrap(new byte[31]));
        Assert.Equal("contentKey32", tooShort.ParamName);
        var tooLong = Assert.Throws<ArgumentException>(() => pair.Wrap(new byte[33]));
        Assert.Equal("contentKey32", tooLong.ParamName);

        var garbage = Assert.Throws<CryptographicException>(() => pair.Unwrap([1, 2, 3]));
        Assert.Equal("The envelope is corrupt.", garbage.Message);
    }

    [Fact]
    public void RsaKey_wrap_round_trips_32_bytes_and_dispose_is_idempotent()
    {
        using var pair = EncryptionRsaKey.Generate(2048);
        var key = new byte[32];
        key[0] = 7;
        key[31] = 9;
        var wrapped = pair.Wrap(key);
        Assert.Equal(key, pair.Unwrap(wrapped));
        var spki = pair.ExportPublicSpki();
        using var fromSpki = EncryptionRsaKey.FromPublicSpki(spki);
        Assert.False(fromSpki.CanUnwrap);
        using var fromPkcs8 = EncryptionRsaKey.FromPkcs8(pair.ExportPkcs8());
        Assert.True(fromPkcs8.CanUnwrap);
        Assert.Equal(key, fromPkcs8.Unwrap(wrapped));

        pair.Dispose();
        pair.Dispose();
        Assert.Throws<ObjectDisposedException>(() => pair.Wrap(key));
        Assert.Throws<ObjectDisposedException>(() => pair.ExportPkcs8());
        Assert.Throws<ObjectDisposedException>(() => pair.Unwrap(wrapped));
    }

    [Fact]
    public void Validate_missing_and_plain_files_do_not_throw()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumEncTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var missing = EncryptionHelper.ValidateFile(Path.Combine(dir, "nope.aes"));
        Assert.Contains(missing.Problems, p => p.Contains("not found", StringComparison.OrdinalIgnoreCase));
        var plain = Path.Combine(dir, "plain.txt");
        File.WriteAllText(plain, "xyz");
        var check = EncryptionHelper.ValidateFile(plain);
        Assert.False(check.IsVestigium);
        Assert.Contains(check.Problems, p => p.Contains("Not a Vestigium envelope", StringComparison.Ordinal));
        Assert.False(EncryptionHelper.IsVestigiumFile(Path.Combine(dir, "missing.bin")));
    }

    [Fact]
    public void Seal_rejects_in_place_empty_wrap_list_and_too_many_wraps()
    {
        using var secret = EncryptionSecret.FromKey(Key());
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumEncTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var src = Path.Combine(dir, "nathan.txt");
        File.WriteAllText(src, "hello");
        var inPlace = Assert.Throws<InvalidOperationException>(() => EncryptionHelper.SealFile(src, src, secret));
        Assert.Contains("In-place", inPlace.Message, StringComparison.Ordinal);
        Assert.Throws<ArgumentException>(() => EncryptionHelper.SealString("x", Array.Empty<EncryptionRsaKey>()));
        using var a = EncryptionRsaKey.Generate(2048);
        using var b = EncryptionRsaKey.Generate(2048);
        using var c = EncryptionRsaKey.Generate(2048);
        using var d = EncryptionRsaKey.Generate(2048);
        using var e = EncryptionRsaKey.Generate(2048);
        using var f = EncryptionRsaKey.Generate(2048);
        using var g = EncryptionRsaKey.Generate(2048);
        using var h = EncryptionRsaKey.Generate(2048);
        using var i = EncryptionRsaKey.Generate(2048);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            EncryptionHelper.SealString("x", [a, b, c, d, e, f, g, h, i]));
    }

    [Fact]
    public void DefaultExportDirectory_contains_exports_folder()
    {
        var dir = EncryptionHelper.DefaultExportDirectory("Encryption");
        Assert.Contains(Path.Combine("Vestigium", "Exports", "Encryption"), dir);
        Assert.Throws<ArgumentException>(() => EncryptionHelper.DefaultExportDirectory(" "));
    }

    private static byte[] Key()
    {
        var key = new byte[32];
        key[0] = 7;
        key[31] = 9;
        return key;
    }

    [Fact]
    public void NewExportPath_stamps_aes_or_argon_and_keeps_original_stem()
    {
        var stamped = EncryptionHelper.NewExportPath("Encryption");
        Assert.Contains("vestigium-Encryption-", Path.GetFileName(stamped));
        Assert.EndsWith(".aes", stamped);
        using var pass = EncryptionSecret.FromPassphrase("gallery-demo-only");
        Assert.EndsWith(".argon", EncryptionHelper.NewExportPath("Encryption", secret: pass));
        Assert.Equal(".argon", EncryptionHelper.FileExtension(pass));
        using var key = EncryptionSecret.FromKey(Key());
        Assert.Equal(".aes", EncryptionHelper.FileExtension(key));
        var named = EncryptionHelper.NewExportPath("Encryption", "nathan.txt", key);
        Assert.Equal("nathan.aes", Path.GetFileName(named));
        Assert.Throws<ArgumentException>(() => EncryptionHelper.NewExportPath(" "));
        Assert.Equal("file", OriginalNames.Stem(".txt"));
        Assert.Equal("nathan", OriginalNames.Stem("nathan.txt"));
        Assert.Throws<ArgumentException>(() => OriginalNames.Validate("a\\b.txt"));
        Assert.Throws<ArgumentException>(() => OriginalNames.Validate("foo..bar.txt"));
        Assert.Throws<ArgumentException>(() => OriginalNames.Validate("x\0y.txt"));
        Assert.Throws<ArgumentException>(() => OriginalNames.Validate(new string('n', 256)));
        Assert.Equal("b.txt", OriginalNames.Validate("a/b.txt"));
    }

    [Fact]
    public void IsVestigium_covers_missing_short_nonseekable_and_header()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumEncTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        Assert.False(EncryptionHelper.TryPeekFile(Path.Combine(dir, "nope.aes"), out _));
        using var zeros = new MemoryStream(new byte[20]);
        Assert.False(EncryptionHelper.IsVestigium(zeros));
        Assert.Equal(0, zeros.Position);
        using var nonSeek = new NonSeekableStream(new byte[32]);
        Assert.False(EncryptionHelper.IsVestigium(nonSeek));

        using var secret = EncryptionSecret.FromKey(Key());
        var sealedPath = Path.Combine(dir, "nathan.aes");
        var src = Path.Combine(dir, "nathan.txt");
        File.WriteAllText(src, "hello");
        EncryptionHelper.SealFile(src, sealedPath, secret);
        Assert.True(EncryptionHelper.IsVestigiumFile(sealedPath));
        Assert.True(EncryptionHelper.TryPeekFile(sealedPath, out var info));
        Assert.NotNull(info);
        using var blob = File.OpenRead(sealedPath);
        Assert.True(EncryptionHelper.IsVestigium(blob));
    }

    private sealed class NonSeekableStream : Stream
    {
        private readonly MemoryStream _inner;
        public NonSeekableStream(byte[] data) => _inner = new MemoryStream(data);
        public override bool CanRead => true;
        public override bool CanSeek => false;
        public override bool CanWrite => false;
        public override long Length => _inner.Length;
        public override long Position { get => _inner.Position; set => throw new NotSupportedException(); }
        public override void Flush() { }
        public override int Read(byte[] buffer, int offset, int count) => _inner.Read(buffer, offset, count);
        public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
        public override void SetLength(long value) => throw new NotSupportedException();
        public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
    }

}
