using System.Security.Cryptography;
using System.Text;
using Vestigium.Helpers.Hashing;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class HashingSessionTests
{
    public HashingSessionTests()
    {
        HelperLog.Shutdown();
    }

    [Fact]
    public void Identity_is_stable()
        => Assert.Equal("Vestigium.Helpers.Hashing", HashingHelper.Identity);

    [Fact]
    public void Sha256_empty_and_abc_match_fips()
    {
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", HashingHelper.HashString(""));
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", HashingHelper.HashString("abc"));
    }

    [Fact]
    public void Sha384_and_sha512_abc_match_fips()
    {
        Assert.Equal(
            "cb00753f45a35e8bb5a03d699ac65007272c32ab0eded1631a8b605a43ff5bed8086072ba1e7cc2358baeca134c825a7",
            HashingHelper.HashString("abc", HashingAlgorithm.Sha384));
        Assert.Equal(
            "ddaf35a193617abacc417349ae20413112e6fa4e89a97ea20a9eeee64b55d39a2192992a274fc1a836ba3c23a3feebbd454d4423643ce80e2a9ac94fa54ca49f",
            HashingHelper.HashString("abc", HashingAlgorithm.Sha512));
    }

    [Fact]
    public void Default_print_is_hex_lower_not_base64()
    {
        var hex = HashingHelper.HashString("abc");
        Assert.Equal(64, hex.Length);
        Assert.Equal(hex, hex.ToLowerInvariant());
        Assert.DoesNotContain('=', hex);
        var b64 = HashingConvert.HexToBase64(hex);
        Assert.Equal("ungWv48Bz+pBQUDeXa4iI7ADYaOWF3qctBD/YfIAFa0=", b64);
        Assert.Equal(hex, HashingConvert.Base64ToHex(b64));
        Assert.Equal(hex, HashingHelper.HashString("abc", HashingAlgorithm.Sha256, HashingTextFormat.HexLower));
        Assert.Equal(b64, HashingHelper.HashString("abc", HashingAlgorithm.Sha256, HashingTextFormat.Base64));
        Assert.Equal(hex.ToUpperInvariant(), HashingHelper.HashString("abc", HashingAlgorithm.Sha256, HashingTextFormat.HexUpper));
    }

    [Fact]
    public void Convert_hex_base64_url_round_trips()
    {
        var hex = HashingHelper.HashString("abc");
        var url = HashingConvert.HexToBase64Url(hex);
        Assert.DoesNotContain('+', url);
        Assert.DoesNotContain('/', url);
        Assert.Equal(hex, HashingConvert.Base64UrlToHex(url));
    }

    [Fact]
    public void Md5_and_sha1_are_named_interop_never_default()
    {
        Assert.Equal("900150983cd24fb0d6963f7d28e17f72", HashingHelper.HashMd5("abc"));
        Assert.Equal("a9993e364706816aba3e25717850c26c9cd0d89d", HashingHelper.HashSha1("abc"));
        Assert.True(HashingHelper.IsInterop(HashingAlgorithm.Md5));
        Assert.False(HashingHelper.IsInterop(HashingAlgorithm.Sha256));
        Assert.Equal(HashingAlgorithm.Sha256, default(HashingAlgorithm) == 0 ? HashingAlgorithm.Sha256 : HashingAlgorithm.Sha256);
    }

    [Fact]
    public void Sha3_abc_matches_fips_when_os_supports_it()
    {
        if (!HashingHelper.IsSupported(HashingAlgorithm.Sha3_256))
            return;

        Assert.Equal(
            "3a985da74fe225b2045c172d6bd390bd855f086e3e9d525b46bfe24511431532",
            HashingHelper.HashString("abc", HashingAlgorithm.Sha3_256));
        Assert.Equal(
            "a7ffc6f8bf1ed76651c14756a061d662f580ff4de43b49fa82d80a4b80f8434a",
            HashingHelper.HashString("", HashingAlgorithm.Sha3_256));
    }

    [Fact]
    public void TryHash_and_hashdata_agree()
    {
        var utf8 = Encoding.UTF8.GetBytes("abc");
        Span<byte> dest = stackalloc byte[32];
        Assert.True(HashingHelper.TryHash(utf8, dest, out var written));
        Assert.Equal(32, written);
        Assert.Equal(HashingHelper.HashData(utf8), dest.ToArray());
        Span<byte> tiny = stackalloc byte[8];
        Assert.False(HashingHelper.TryHash(utf8, tiny, out var none));
        Assert.Equal(0, none);
    }

    [Fact]
    public void File_over_64kib_matches_bcl()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHashTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "nathan.txt");
        var payload = new byte[200_000];
        RandomNumberGenerator.Fill(payload);
        File.WriteAllBytes(path, payload);
        try
        {
            var hex = HashingHelper.HashFile(path);
            Assert.Equal(Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant(), hex);
            Assert.True(HashingHelper.VerifyFile(path, hex));
            Assert.False(HashingHelper.VerifyFile(path, new string('0', 64)));
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Hmac_rfc4231_case1_and_key_sizes()
    {
        var key = HmacKey.FromBytes(Enumerable.Repeat((byte)0x0b, 20).ToArray());
        var mac = HashingHelper.HmacString("Hi There", key);
        Assert.Equal("b0344c61d8db38535ca8afceaf0bf12b881dc200c9833da726e9376c2e32cff7", mac);
        Assert.True(HashingHelper.VerifyHmacString("Hi There", key, mac));
        Assert.False(HashingHelper.VerifyHmacString("Hi there", key, mac));

        Assert.Throws<ArgumentException>(() => HmacKey.FromString("short"));
        using var generated = HmacKey.Generate(HmacKeySize.Bytes32);
        Assert.Equal(32, generated.Length);
        using var sized = HmacKey.FromString("sixteen-byte-key", HmacKeySize.Bytes16);
        Assert.Equal(16, sized.Length);
        Assert.Throws<ArgumentException>(() => HmacKey.FromString("sixteen-byte-key", HmacKeySize.Bytes32));
        using var g16 = HmacKey.Generate(HmacKeySize.Bytes16);
        using var g64 = HmacKey.Generate(HmacKeySize.Bytes64);
        using var g128 = HmacKey.Generate(HmacKeySize.Bytes128);
        Assert.Equal(16, g16.Length);
        Assert.Equal(64, g64.Length);
        Assert.Equal(128, g128.Length);
    }

    [Fact]
    public void Hmac_rfc4231_case1_sha384_and_sha512()
    {
        var key = HmacKey.FromBytes(Enumerable.Repeat((byte)0x0b, 20).ToArray());
        Assert.Equal(
            "afd03944d84895626b0825f4ab46907f15f9dadbe4101ec682aa034c7cebc59cfaea9ea9076ede7f4af152e8b2fa9cb6",
            HashingHelper.HmacString("Hi There", key, HmacAlgorithm.Sha384));
        Assert.Equal(
            "87aa7cdea5ef619d4ff0b4241a1d6cb02379f4e2ce4ec2787ad0b30545e17cdedaa833b7d6b8a702038b274eaea3f4e4be9d914eeb61f1702e696c203a126854",
            HashingHelper.HmacString("Hi There", key, HmacAlgorithm.Sha512));
        Assert.Equal(32, HashingHelper.HmacLength(HmacAlgorithm.Sha256));
        Assert.Equal(48, HashingHelper.HmacLength(HmacAlgorithm.Sha384));
        Assert.Equal(64, HashingHelper.HmacLength(HmacAlgorithm.Sha512));
        var sha256 = HashingHelper.HmacString("Hi There", key);
        Assert.NotEqual(sha256, HashingHelper.HmacString("Hi There", key, HmacAlgorithm.Sha384));
        Assert.True(HashingHelper.VerifyHmacString(
            "Hi There",
            key,
            "afd03944d84895626b0825f4ab46907f15f9dadbe4101ec682aa034c7cebc59cfaea9ea9076ede7f4af152e8b2fa9cb6",
            HmacAlgorithm.Sha384));
        Assert.False(HashingHelper.VerifyHmacString("Hi There", key, sha256, HmacAlgorithm.Sha384));
    }

    [Fact]
    public void Hmac_sha3_rfc4231_style_when_os_supports_it()
    {
        if (!HashingHelper.IsHmacSupported(HmacAlgorithm.Sha3_256))
            return;

        var key = HmacKey.FromBytes(Enumerable.Repeat((byte)0x0b, 20).ToArray());
        Assert.Equal(
            "ba85192310dffa96e2a3a40e69774351140bb7185e1202cdcc917589f95e16bb",
            HashingHelper.HmacString("Hi There", key, HmacAlgorithm.Sha3_256));
        Assert.Equal(
            "68d2dcf7fd4ddd0a2240c8a437305f61fb7334cfb5d0226e1bc27dc10a2e723a20d370b47743130e26ac7e3d532886bd",
            HashingHelper.HmacString("Hi There", key, HmacAlgorithm.Sha3_384));
        Assert.Equal(
            "eb3fbd4b2eaab8f5c504bd3a41465aacec15770a7cabac531e482f860b5ec7ba47ccb2c6f2afce8f88d22b6dc61380f23a668fd3888bb80537c0a0b86407689e",
            HashingHelper.HmacString("Hi There", key, HmacAlgorithm.Sha3_512));
        var sha256 = HashingHelper.HmacString("Hi There", key);
        Assert.NotEqual(sha256, HashingHelper.HmacString("Hi There", key, HmacAlgorithm.Sha3_256));
        Assert.Equal(32, HashingHelper.HmacLength(HmacAlgorithm.Sha3_256));
        Assert.Equal(48, HashingHelper.HmacLength(HmacAlgorithm.Sha3_384));
        Assert.Equal(64, HashingHelper.HmacLength(HmacAlgorithm.Sha3_512));
        Assert.True(HashingHelper.VerifyHmacString(
            "Hi There",
            key,
            "ba85192310dffa96e2a3a40e69774351140bb7185e1202cdcc917589f95e16bb",
            HmacAlgorithm.Sha3_256));
    }

    [Fact]
    public void Shake_fips202_abc_when_os_supports_it()
    {
        if (!HashingHelper.IsShakeSupported)
            return;

        Assert.Equal(
            "5881092dd818bf5cf8a3ddb793fbcba74097d5c526a6d35f97b83351940f2cc8",
            HashingHelper.Shake128("abc"));
        Assert.Equal(
            "483366601360a8771c6863080cc4114d8db44530f8f1e1ee4f94ea37e78b5739d5a15bef186a5386c75744c0527e1faa9f8726e462a12a4feb06bd8801e751e4",
            HashingHelper.Shake256("abc"));
        Assert.Equal(32, HashingHelper.ShakeDefaultLength(ShakeAlgorithm.Shake128));
        Assert.Equal(64, HashingHelper.ShakeDefaultLength(ShakeAlgorithm.Shake256));
        Assert.NotEqual(HashingHelper.HashString("abc"), HashingHelper.Shake128("abc"));
        Assert.True(HashingHelper.VerifyShakeString("abc", HashingHelper.Shake128("abc")));
        Assert.False(HashingHelper.VerifyShakeString("abd", HashingHelper.Shake128("abc")));
        Assert.Equal(64, HashingHelper.ShakeString("abc", ShakeAlgorithm.Shake128, 32).Length);
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            HashingHelper.ShakeString("abc", ShakeAlgorithm.Shake128, 0));
    }

    [Fact]
    public void Kmac_nist_sp800185_sample1_when_os_supports_it()
    {
        if (!HashingHelper.IsKmacSupported)
            return;

        var keyBytes = Enumerable.Range(0x40, 32).Select(i => (byte)i).ToArray();
        using var key = HmacKey.FromBytes(keyBytes);
        ReadOnlySpan<byte> msg = [0x00, 0x01, 0x02, 0x03];
        Assert.Equal(
            "e5780b0d3ea6f7d3a429c5706aa43a00fadbd7d49628839e3187243f456ee14e",
            HashingHelper.KmacBytes(msg, key, KmacAlgorithm.Kmac128));
        Assert.Equal(
            "2ebd1622de2de44174e3477206060d7f64489a639b7545649132317609fa214f4c8ac90630fb4c757fba074b15186fe452ae71b6a1e443bf54059e090c11ae20",
            HashingHelper.KmacBytes(msg, key, KmacAlgorithm.Kmac256));
        var tagged = Encoding.UTF8.GetBytes("My Tagged Application");
        Assert.Equal(
            "3b1fba963cd8b0b59e8c1a6d71888b7143651af8ba0a7070c0979e2811324aa5",
            HashingHelper.KmacBytes(msg, key, KmacAlgorithm.Kmac128, 32, HashingTextFormat.HexLower, tagged));
        var hmac = HashingHelper.HmacBytes(msg, key);
        Assert.NotEqual(hmac, HashingHelper.KmacBytes(msg, key));
        Assert.True(HashingHelper.VerifyKmacString("Hi There", key, HashingHelper.KmacString("Hi There", key)));
        Assert.Equal(32, HashingHelper.KmacDefaultLength(KmacAlgorithm.Kmac128));
        Assert.Equal(64, HashingHelper.KmacDefaultLength(KmacAlgorithm.Kmac256));
    }

    [Fact]
    public void Kmac_and_shake_file_over_64kib_matches_in_memory()
    {
        if (!HashingHelper.IsKmacSupported && !HashingHelper.IsShakeSupported)
            return;

        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHashTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "nathan.bin");
        var payload = new byte[200_000];
        RandomNumberGenerator.Fill(payload);
        File.WriteAllBytes(path, payload);
        using var key = HmacKey.Generate();
        try
        {
            if (HashingHelper.IsKmacSupported)
            {
                foreach (var alg in new[] { KmacAlgorithm.Kmac128, KmacAlgorithm.Kmac256 })
                {
                    var fileHex = HashingHelper.KmacFile(path, key, alg);
                    var memHex = HashingHelper.KmacBytes(payload, key, alg);
                    Assert.Equal(memHex, fileHex);
                    Assert.True(HashingHelper.VerifyKmacFile(path, key, fileHex, alg));
                }
            }

            if (HashingHelper.IsShakeSupported)
            {
                foreach (var alg in new[] { ShakeAlgorithm.Shake128, ShakeAlgorithm.Shake256 })
                {
                    var fileHex = HashingHelper.ShakeFile(path, alg);
                    var memHex = HashingHelper.ShakeBytes(payload, alg);
                    Assert.Equal(memHex, fileHex);
                    Assert.True(HashingHelper.VerifyShakeFile(path, fileHex, alg));
                }
            }
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Hmac_file_over_64kib_matches_in_memory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHashTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "nathan.bin");
        var payload = new byte[200_000];
        RandomNumberGenerator.Fill(payload);
        File.WriteAllBytes(path, payload);
        using var key = HmacKey.Generate();
        try
        {
            foreach (var alg in new[] { HmacAlgorithm.Sha256, HmacAlgorithm.Sha384, HmacAlgorithm.Sha512 })
            {
                var fileHex = HashingHelper.HmacFile(path, key, alg);
                var memHex = HashingHelper.HmacBytes(payload, key, alg);
                Assert.Equal(memHex, fileHex);
                Assert.True(HashingHelper.VerifyHmacFile(path, key, fileHex, alg));
            }

            if (HashingHelper.IsHmacSupported(HmacAlgorithm.Sha3_256))
            {
                foreach (var alg in new[] { HmacAlgorithm.Sha3_256, HmacAlgorithm.Sha3_384, HmacAlgorithm.Sha3_512 })
                {
                    var fileHex = HashingHelper.HmacFile(path, key, alg);
                    var memHex = HashingHelper.HmacBytes(payload, key, alg);
                    Assert.Equal(memHex, fileHex);
                    Assert.True(HashingHelper.VerifyHmacFile(path, key, fileHex, alg));
                }
            }
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Hmac_from_base64_does_not_utf8_the_letters()
    {
        using var key = HmacKey.Generate();
        var b64 = key.ToBase64();
        using var decoded = HmacKey.FromBase64(b64);
        Assert.Equal(key.ToHexLower(), decoded.ToHexLower());
        using var wrong = HmacKey.FromString(b64);
        Assert.NotEqual(key.ToHexLower(), wrong.ToHexLower());
        Assert.True(wrong.Length >= 16);
    }

    [Fact]
    public void Password_argon2id_phc_round_trips_and_rejects_wrong()
    {
        var stored = HashingHelper.HashPassword("gallery-demo-only");
        Assert.StartsWith("$argon2id$v=19$m=19456,t=2,p=1$", stored);
        Assert.True(HashingHelper.VerifyPassword("gallery-demo-only", stored));
        Assert.False(HashingHelper.VerifyPassword("gallery-demo-xx", stored));
        Assert.Throws<ArgumentException>(() => HashingHelper.HashPassword("short"));
        Assert.Throws<FormatException>(() => HashingHelper.VerifyPassword("gallery-demo-only", "not-a-phc"));
    }

    [Fact]
    public void Logs_never_contain_password_phc_or_hmac_key()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHashTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        HelperLog.InitializeHost(HelperLog.AppIds.Hashing, cfg =>
        {
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
        });
        try
        {
            var stored = HashingHelper.HashPassword("gallery-demo-only");
            using var key = HmacKey.Generate();
            var keyHex = key.ToHexLower();
            var keyB64 = key.ToBase64();
            _ = HashingHelper.HmacString("probe-message-16", key);
            _ = HashingHelper.HashString("abc");
            var blob = string.Join('\n', HelperLog.RecentJsonLines);
            Assert.DoesNotContain("gallery-demo-only", blob, StringComparison.Ordinal);
            Assert.DoesNotContain(stored, blob, StringComparison.Ordinal);
            Assert.DoesNotContain("$argon2id$", blob, StringComparison.Ordinal);
            Assert.DoesNotContain(keyHex, blob, StringComparison.OrdinalIgnoreCase);
            Assert.DoesNotContain(keyB64, blob, StringComparison.Ordinal);
            Assert.Contains("password hashed", blob, StringComparison.Ordinal);
            Assert.Contains("\"APPID\":\"Hashing\"", blob, StringComparison.Ordinal);
        }
        finally
        {
            HelperLog.Shutdown();
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Verify_string_constant_time_mismatch()
    {
        var hex = HashingHelper.HashString("abc");
        Assert.True(HashingHelper.VerifyString("abc", hex));
        Assert.False(HashingHelper.VerifyString("abd", hex));
        var b64 = HashingConvert.HexToBase64(hex);
        Assert.True(HashingHelper.VerifyString("abc", b64, HashingAlgorithm.Sha256, HashingTextFormat.Base64));
    }

    [Fact]
    public void Checksum_crc32_crc64_xxhash_published_vectors()
    {
        Assert.Equal("00000000", HashingHelper.ChecksumCrc32(""));
        Assert.Equal("cbf43926", HashingHelper.ChecksumCrc32("123456789"));
        Assert.Equal("0000000000000000", HashingHelper.ChecksumCrc64(""));
        Assert.Equal("6c40df5f0b497347", HashingHelper.ChecksumCrc64("123456789"));
        Assert.Equal("02cc5d05", HashingHelper.ChecksumString("", ChecksumAlgorithm.XxHash32));
        Assert.Equal("32d153ff", HashingHelper.ChecksumString("abc", ChecksumAlgorithm.XxHash32));
        Assert.Equal("ef46db3751d8e999", HashingHelper.ChecksumXxHash(""));
        Assert.Equal("44bc2cf5ad770999", HashingHelper.ChecksumXxHash("abc"));
        Assert.Equal("d24ec4f1a98c6e5b", HashingHelper.ChecksumXxHash("a"));
        Assert.Equal("02a2e85470d6fd96", HashingHelper.ChecksumXxHash("Call me Ishmael. Some years ago--never mind how long precisely-"));
        Assert.Equal("2d06800538d394c2", HashingHelper.ChecksumString("", ChecksumAlgorithm.XxHash3));
        Assert.Equal("78af5f94892f3950", HashingHelper.ChecksumString("abc", ChecksumAlgorithm.XxHash3));
        Assert.True(HashingHelper.VerifyChecksumString("123456789", "cbf43926"));
        Assert.False(HashingHelper.VerifyChecksumString("123456788", "cbf43926"));
        Assert.Equal(4, HashingHelper.ChecksumLength(ChecksumAlgorithm.Crc32));
        Assert.Equal(8, HashingHelper.ChecksumLength(ChecksumAlgorithm.XxHash64));
    }

    [Fact]
    public void Checksum_file_over_64kib_matches_in_memory()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHashTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "nathan.bin");
        var payload = new byte[200_000];
        RandomNumberGenerator.Fill(payload);
        File.WriteAllBytes(path, payload);
        try
        {
            foreach (var alg in new[] { ChecksumAlgorithm.Crc32, ChecksumAlgorithm.Crc64, ChecksumAlgorithm.XxHash64, ChecksumAlgorithm.XxHash3 })
            {
                var fileHex = HashingHelper.ChecksumFile(path, alg);
                var memHex = HashingHelper.ChecksumBytes(payload, alg);
                Assert.Equal(memHex, fileHex);
                Assert.True(HashingHelper.VerifyChecksumFile(path, fileHex, alg));
            }
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Checksum_is_not_hash_and_never_the_default()
    {
        var hash = HashingHelper.HashString("123456789");
        var crc = HashingHelper.ChecksumCrc32("123456789");
        Assert.NotEqual(hash, crc);
        Assert.Equal(64, hash.Length);
        Assert.Equal(8, crc.Length);
        Assert.Equal("cbf43926", crc);
    }
}
