using System.Security.Cryptography;
using System.Text;
using Vestigium.Helpers.Hashing;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class HashingBranchTests
{
    static readonly HashingAlgorithm[] Digests =
    [
        HashingAlgorithm.Sha256,
        HashingAlgorithm.Sha384,
        HashingAlgorithm.Sha512,
        HashingAlgorithm.Sha3_256,
        HashingAlgorithm.Sha3_384,
        HashingAlgorithm.Sha3_512,
        HashingAlgorithm.Md5,
        HashingAlgorithm.Sha1,
    ];

    static readonly ChecksumAlgorithm[] Checksums =
    [
        ChecksumAlgorithm.Crc32,
        ChecksumAlgorithm.Crc64,
        ChecksumAlgorithm.XxHash32,
        ChecksumAlgorithm.XxHash64,
        ChecksumAlgorithm.XxHash3,
    ];

    static readonly HmacAlgorithm[] Hmacs =
    [
        HmacAlgorithm.Sha256,
        HmacAlgorithm.Sha384,
        HmacAlgorithm.Sha512,
        HmacAlgorithm.Sha3_256,
        HmacAlgorithm.Sha3_384,
        HmacAlgorithm.Sha3_512,
    ];

    [Fact]
    public void Algorithm_metadata_covers_every_named_digest_and_rejects_garbage()
    {
        foreach (var alg in Digests)
        {
            var name = HashingHelper.AlgorithmName(alg);
            Assert.False(string.IsNullOrWhiteSpace(name));
            var length = HashingHelper.DigestLength(alg);
            Assert.True(length > 0);
            var supported = HashingHelper.IsSupported(alg);
            if (supported)
            {
                var hex = HashingHelper.HashBytes("abc"u8, alg);
                Assert.Equal(length * 2, hex.Length);
            }
        }

        Assert.True(HashingHelper.IsInterop(HashingAlgorithm.Sha1));
        Assert.False(HashingHelper.IsInterop(HashingAlgorithm.Sha384));
        Assert.False(HashingHelper.IsSupported((HashingAlgorithm)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => HashingHelper.AlgorithmName((HashingAlgorithm)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => HashingHelper.DigestLength((HashingAlgorithm)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => HashingHelper.HashData([1], (HashingAlgorithm)99));
    }

    [Fact]
    public void Checksum_metadata_covers_every_named_algorithm_and_rejects_garbage()
    {
        foreach (var alg in Checksums)
        {
            Assert.False(string.IsNullOrWhiteSpace(HashingHelper.ChecksumName(alg)));
            Assert.True(HashingHelper.ChecksumLength(alg) is 4 or 8);
            var digest = HashingHelper.ChecksumData("abc"u8, alg);
            Assert.Equal(HashingHelper.ChecksumLength(alg), digest.Length);
            Assert.Equal(digest.Length * 2, HashingHelper.ChecksumBytes("abc"u8, alg).Length);
        }

        Assert.Throws<ArgumentOutOfRangeException>(() => HashingHelper.ChecksumName((ChecksumAlgorithm)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => HashingHelper.ChecksumLength((ChecksumAlgorithm)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => HashingHelper.ChecksumData([1], (ChecksumAlgorithm)99));
    }

    [Fact]
    public void Hmac_metadata_covers_every_named_algorithm_and_rejects_garbage()
    {
        foreach (var alg in Hmacs)
        {
            var name = HashingHelper.HmacName(alg);
            Assert.StartsWith("HMAC-", name);
            Assert.True(HashingHelper.HmacLength(alg) is 32 or 48 or 64);
            _ = HashingHelper.IsHmacSupported(alg);
        }

        Assert.False(HashingHelper.IsHmacSupported((HmacAlgorithm)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => HashingHelper.HmacName((HmacAlgorithm)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => HashingHelper.HmacLength((HmacAlgorithm)99));
        using var key = HmacKey.Generate();
        Assert.Throws<ArgumentOutOfRangeException>(() => HashingHelper.HmacData("x"u8, key, (HmacAlgorithm)99));
    }

    [Fact]
    public void Kmac_and_shake_names_reject_garbage_and_name_the_known_ids()
    {
        Assert.Equal("KMAC128", HashingHelper.KmacName(KmacAlgorithm.Kmac128));
        Assert.Equal("KMAC256", HashingHelper.KmacName(KmacAlgorithm.Kmac256));
        Assert.Throws<ArgumentOutOfRangeException>(() => HashingHelper.KmacName((KmacAlgorithm)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => HashingHelper.KmacDefaultLength((KmacAlgorithm)99));

        Assert.Equal("SHAKE128", HashingHelper.ShakeName(ShakeAlgorithm.Shake128));
        Assert.Equal("SHAKE256", HashingHelper.ShakeName(ShakeAlgorithm.Shake256));
        Assert.Throws<ArgumentOutOfRangeException>(() => HashingHelper.ShakeName((ShakeAlgorithm)99));
        Assert.Throws<ArgumentOutOfRangeException>(() => HashingHelper.ShakeDefaultLength((ShakeAlgorithm)99));
    }

    [Fact]
    public async Task HashFileAsync_and_ChecksumFileAsync_match_the_sync_stream_overloads()
    {
        var payload = Encoding.UTF8.GetBytes("branch-coverage-payload-" + new string('n', 80_000));
        using (var stream = new MemoryStream(payload, writable: false))
        {
            var sync = HashingHelper.HashFile(stream, HashingAlgorithm.Sha256, HashingTextFormat.HexLower);
            stream.Position = 0;
            var asyncHex = await HashingHelper.HashFileAsync(stream);
            Assert.Equal(sync, asyncHex);
        }

        using (var stream = new MemoryStream(payload, writable: false))
        {
            var crcSync = HashingHelper.ChecksumFile(stream, ChecksumAlgorithm.Crc32);
            stream.Position = 0;
            var crcAsync = await HashingHelper.ChecksumFileAsync(stream);
            Assert.Equal(crcSync, crcAsync);
        }

        using (var stream = new MemoryStream(payload, writable: false))
        {
            var xx = await HashingHelper.ChecksumFileAsync(stream, ChecksumAlgorithm.XxHash64, HashingTextFormat.HexLower);
            Assert.Equal(16, xx.Length);
        }

        Assert.Throws<ArgumentNullException>(() => HashingHelper.HashFile((Stream)null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => HashingHelper.HashFileAsync(null!));
        await Assert.ThrowsAsync<ArgumentNullException>(() => HashingHelper.ChecksumFileAsync(null!));
    }

    [Fact]
    public void VerifyBytes_and_stream_without_path_name()
    {
        var utf8 = Encoding.UTF8.GetBytes("abc");
        var hex = HashingHelper.HashBytes(utf8);
        Assert.True(HashingHelper.VerifyBytes(utf8, hex));
        Assert.False(HashingHelper.VerifyBytes(utf8, new string('0', 64)));
        var b64 = HashingConvert.HexToBase64(hex);
        Assert.True(HashingHelper.VerifyBytes(utf8, b64, HashingAlgorithm.Sha256, HashingTextFormat.Base64));

        using var stream = new MemoryStream(utf8, writable: false);
        var hashed = HashingHelper.HashFile(stream);
        Assert.Equal(hex, hashed);
        stream.Position = 0;
        var crc = HashingHelper.ChecksumFile(stream, ChecksumAlgorithm.Crc32);
        Assert.Equal(8, crc.Length);
    }

    [Fact]
    public void Convert_empty_prefix_padding_and_invalid_format()
    {
        Assert.Equal("", HashingConvert.Format([], HashingTextFormat.HexLower));
        Assert.Equal("", HashingConvert.Format([], HashingTextFormat.HexUpper));
        Assert.Equal("", HashingConvert.Format([], HashingTextFormat.Base64));
        Assert.Equal("", HashingConvert.Format([], HashingTextFormat.Base64Url));
        Assert.Throws<ArgumentOutOfRangeException>(() => HashingConvert.Format([1], (HashingTextFormat)99));

        Assert.Empty(HashingConvert.Parse("", HashingTextFormat.HexLower));
        Assert.Empty(HashingConvert.Parse("   ", HashingTextFormat.Base64));
        var hex = HashingHelper.HashString("abc");
        Assert.Equal(Convert.FromHexString(hex), HashingConvert.Parse("0x" + hex, HashingTextFormat.HexLower));
        Assert.Equal(Convert.FromHexString(hex), HashingConvert.Parse("0X" + hex, HashingTextFormat.HexUpper));
        var dashed = string.Join("-", Enumerable.Range(0, 32).Select(i => hex.Substring(i * 2, 2)));
        Assert.Equal(Convert.FromHexString(hex), HashingConvert.Parse(dashed, HashingTextFormat.HexLower));
        var spaced = string.Join(" ", Enumerable.Range(0, 32).Select(i => hex.Substring(i * 2, 2)));
        Assert.Equal(Convert.FromHexString(hex), HashingConvert.Parse(spaced, HashingTextFormat.HexLower));

        var url = HashingConvert.HexToBase64Url(hex);
        Assert.Equal(Convert.FromHexString(hex), HashingConvert.Parse(url, HashingTextFormat.Base64Url));
        Assert.Throws<ArgumentOutOfRangeException>(() => HashingConvert.Parse("aa", (HashingTextFormat)99));
        Assert.Throws<FormatException>(() => HashingConvert.Parse("zz", HashingTextFormat.HexLower));
        Assert.Throws<FormatException>(() => HashingConvert.Parse("@@@", HashingTextFormat.Base64));
    }

    [Fact]
    public void Password_verifier_rejects_each_phc_failure_arm()
    {
        const string pwd = "gallery-demo-only";
        Assert.Throws<FormatException>(() => HashingHelper.VerifyPassword(pwd, "$argon2i$v=19$m=19456,t=2,p=1$c2FsdHNhbHQ$c2FsdHNhbHRzYWx0c2FsdHNhbHRzYWx0c2FsdA"));
        Assert.Throws<FormatException>(() => HashingHelper.VerifyPassword(pwd, "$argon2id$v=16$m=19456,t=2,p=1$c2FsdHNhbHQ$c2FsdHNhbHRzYWx0c2FsdHNhbHRzYWx0c2FsdA"));
        Assert.Throws<FormatException>(() => HashingHelper.VerifyPassword(pwd, "$argon2id$v=19$m19456$c2FsdHNhbHQ$c2FsdHNhbHRzYWx0c2FsdHNhbHRzYWx0c2FsdA"));
        Assert.Throws<FormatException>(() => HashingHelper.VerifyPassword(pwd, "$argon2id$v=19$m=0,t=2,p=1$c2FsdHNhbHQ$c2FsdHNhbHRzYWx0c2FsdHNhbHRzYWx0c2FsdA"));
        Assert.Throws<FormatException>(() => HashingHelper.VerifyPassword(pwd, "$argon2id$v=19$m=19456,t=2,p=1,q=1$c2FsdHNhbHQ$c2FsdHNhbHRzYWx0c2FsdHNhbHRzYWx0c2FsdA"));
        Assert.Throws<FormatException>(() => HashingHelper.VerifyPassword(pwd, "$argon2id$v=19$m=19456,p=1$c2FsdHNhbHQ$c2FsdHNhbHRzYWx0c2FsdHNhbHRzYWx0c2FsdA"));
        Assert.Throws<FormatException>(() => HashingHelper.VerifyPassword(pwd, "$argon2id$v=19$t=2,p=1$c2FsdHNhbHQ$c2FsdHNhbHRzYWx0c2FsdHNhbHRzYWx0c2FsdA"));
        Assert.Throws<FormatException>(() => HashingHelper.VerifyPassword(pwd, "$argon2id$v=19$m=19456,t=2$c2FsdHNhbHQ$c2FsdHNhbHRzYWx0c2FsdHNhbHRzYWx0c2FsdA"));
        Assert.Throws<FormatException>(() => HashingHelper.VerifyPassword(pwd, "$argon2id$v=19$m=19456,t=2,p=1$%%%$c2FsdHNhbHRzYWx0c2FsdHNhbHRzYWx0c2FsdA"));

        var salt8 = Convert.ToBase64String(new byte[8]).TrimEnd('=');
        var salt4 = Convert.ToBase64String(new byte[4]).TrimEnd('=');
        var hash32 = Convert.ToBase64String(new byte[32]).TrimEnd('=');
        var hash16 = Convert.ToBase64String(new byte[16]).TrimEnd('=');
        Assert.Throws<FormatException>(() => HashingHelper.VerifyPassword(pwd, $"$argon2id$v=19$m=19456,t=2,p=1${salt4}${hash32}"));
        Assert.Throws<FormatException>(() => HashingHelper.VerifyPassword(pwd, $"$argon2id$v=19$m=19456,t=2,p=1${salt8}${hash16}"));

        Assert.Throws<CryptographicException>(() => HashingHelper.VerifyPassword(pwd, $"$argon2id$v=19$m=65537,t=2,p=1${salt8}${hash32}"));
        Assert.Throws<CryptographicException>(() => HashingHelper.VerifyPassword(pwd, $"$argon2id$v=19$m=19456,t=11,p=1${salt8}${hash32}"));
        Assert.Throws<CryptographicException>(() => HashingHelper.VerifyPassword(pwd, $"$argon2id$v=19$m=19456,t=2,p=5${salt8}${hash32}"));
        Assert.Throws<CryptographicException>(() => HashingHelper.VerifyPassword(pwd, $"$argon2id$v=19$m=7,t=2,p=1${salt8}${hash32}"));

        Assert.Throws<ArgumentException>(() => HashingHelper.HashPassword(new string('x', 129)));
        Assert.Throws<ArgumentException>(() => HashingHelper.VerifyPassword("short", $"$argon2id$v=19$m=19456,t=2,p=1${salt8}${hash32}"));
    }

    [Fact]
    public void HashPassword_too_short_is_logged_as_failed()
    {
        Assert.Throws<ArgumentException>(() => HashingHelper.HashPassword(""));
        Assert.Throws<ArgumentException>(() => HashingHelper.VerifyPassword("gallery-demo-only", ""));
    }

    [Fact]
    public void TryHash_file_verify_and_convenience_checksums()
    {
        var utf8 = Encoding.UTF8.GetBytes("abc");
        Assert.False(HashingHelper.TryHash(utf8, stackalloc byte[1], out var written));
        Assert.Equal(0, written);
        Span<byte> dest = stackalloc byte[32];
        Assert.True(HashingHelper.TryHash(utf8, dest, out written));
        Assert.Equal(32, written);

        Assert.Equal(32, HashingHelper.HashMd5("abc").Length);
        Assert.Equal(40, HashingHelper.HashSha1("abc").Length);
        Assert.Equal(8, HashingHelper.ChecksumCrc32("123456789").Length);
        Assert.Equal(16, HashingHelper.ChecksumCrc64("123456789").Length);
        Assert.Equal(16, HashingHelper.ChecksumXxHash("123456789").Length);
        Assert.True(HashingHelper.VerifyChecksumString("123456789", HashingHelper.ChecksumCrc32("123456789")));
        Assert.False(HashingHelper.VerifyChecksumString("123456789", "00000000"));
        Assert.True(HashingHelper.VerifyString("abc", HashingHelper.HashString("abc")));
        Assert.False(HashingHelper.VerifyString("abc", new string('0', 64)));

        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHashTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "payload.bin");
        File.WriteAllBytes(path, utf8);
        var hex = HashingHelper.HashFile(path);
        Assert.True(HashingHelper.VerifyFile(path, hex));
        Assert.False(HashingHelper.VerifyFile(path, new string('0', 64)));
        var crc = HashingHelper.ChecksumFile(path);
        Assert.True(HashingHelper.VerifyChecksumFile(path, crc));
        Assert.False(HashingHelper.VerifyChecksumFile(path, "00000000"));

        foreach (var format in new[] { HashingTextFormat.HexLower, HashingTextFormat.HexUpper, HashingTextFormat.Base64, HashingTextFormat.Base64Url })
        {
            var printed = HashingHelper.HashString("abc", HashingAlgorithm.Sha256, format);
            Assert.True(HashingHelper.VerifyString("abc", printed, HashingAlgorithm.Sha256, format));
        }

        var plusSlash = HashingConvert.Format([0xfb, 0xff, 0xef], HashingTextFormat.Base64);
        Assert.Contains("+", plusSlash);
        Assert.Equal([0xfb, 0xff, 0xef], HashingConvert.Parse(HashingConvert.Format([0xfb, 0xff, 0xef], HashingTextFormat.Base64Url), HashingTextFormat.Base64Url));
        Assert.Equal(HashingConvert.HexToBase64("aa"), HashingConvert.Format([0xaa], HashingTextFormat.Base64));
        Assert.Equal("aa", HashingConvert.Base64ToHex(HashingConvert.HexToBase64("aa")));
        Assert.Equal("aa", HashingConvert.Base64UrlToHex(HashingConvert.HexToBase64Url("aa")));
    }

    [Fact]
    public async Task HashFileAsync_with_path_name_and_hmac_kmac_shake_when_supported()
    {
        var utf8 = Encoding.UTF8.GetBytes("branch-async");
        using (var stream = new MemoryStream(utf8, writable: false))
        {
            var hex = await HashingHelper.HashFileAsync(stream, HashingAlgorithm.Sha256, HashingTextFormat.HexLower, "probe.bin");
            Assert.Equal(64, hex.Length);
        }

        using (var stream = new MemoryStream(utf8, writable: false))
        {
            var crc = await HashingHelper.ChecksumFileAsync(stream, ChecksumAlgorithm.Crc32, HashingTextFormat.HexLower, "probe.bin");
            Assert.Equal(8, crc.Length);
        }

        using var cancelled = new CancellationTokenSource();
        cancelled.Cancel();
        using (var stream = new MemoryStream(utf8, writable: false))
        {
            await Assert.ThrowsAnyAsync<OperationCanceledException>(() =>
                HashingHelper.HashFileAsync(stream, cancellationToken: cancelled.Token));
        }

        using var key = HmacKey.Generate();
        foreach (var alg in Hmacs)
        {
            if (!HashingHelper.IsHmacSupported(alg))
                continue;
            var mac = HashingHelper.HmacString("msg-16-characters", key, alg);
            Assert.Equal(HashingHelper.HmacLength(alg) * 2, mac.Length);
        }

        using var k16 = HmacKey.Generate(HmacKeySize.Bytes16);
        using var k64 = HmacKey.Generate(HmacKeySize.Bytes64);
        using var k128 = HmacKey.Generate(HmacKeySize.Bytes128);
        Assert.Equal(16, k16.Length);
        Assert.Equal(64, k64.Length);
        Assert.Equal(128, k128.Length);
        Assert.Throws<ArgumentOutOfRangeException>(() => HmacKey.Generate((HmacKeySize)99));
        using var fromText = HmacKey.FromString("gallery-demo-key!");
        Assert.True(fromText.Length >= 16);
        _ = fromText.ToHexLower();
        _ = fromText.ToBase64();
        using var fromB64 = HmacKey.FromBase64(Convert.ToBase64String(new byte[32]));
        Assert.Equal(32, fromB64.Length);
        Assert.Throws<ArgumentException>(() => HmacKey.FromBase64("@@@"));
        Assert.Throws<ArgumentException>(() => HmacKey.FromString("short"));

        if (HashingHelper.IsKmacSupported)
        {
            Assert.Equal(64, HashingHelper.Kmac128("abc", key).Length);
            Assert.Equal(128, HashingHelper.Kmac256("abc", key).Length);
            var kmac = HashingHelper.KmacString("abc", key);
            Assert.False(string.IsNullOrWhiteSpace(kmac));
            var bytes = HashingHelper.KmacBytes(utf8, key, KmacAlgorithm.Kmac128);
            Assert.Equal(64, bytes.Length);
            Assert.Throws<ArgumentOutOfRangeException>(() => HashingHelper.KmacData(utf8, key, (KmacAlgorithm)99));
        }

        if (HashingHelper.IsShakeSupported)
        {
            Assert.Equal(64, HashingHelper.Shake128("abc").Length);
            Assert.Equal(128, HashingHelper.Shake256("abc").Length);
            var shake = HashingHelper.ShakeString("abc");
            Assert.True(HashingHelper.VerifyShakeString("abc", shake));
            Assert.False(HashingHelper.VerifyShakeString("abc", new string('0', 64)));
            using var stream = new MemoryStream(utf8, writable: false);
            var fromStream = HashingHelper.ShakeFile(stream, ShakeAlgorithm.Shake128, 32);
            Assert.Equal(64, fromStream.Length);
            Assert.Throws<ArgumentOutOfRangeException>(() => HashingHelper.ShakeData(utf8, (ShakeAlgorithm)99));
        }
    }
}
