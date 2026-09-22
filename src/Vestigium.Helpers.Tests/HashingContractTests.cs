using System.Security.Cryptography;
using Vestigium.Helpers.Hashing;

namespace Vestigium.Helpers.Tests;

public sealed class HashingContractTests
{
    [Fact]
    public void Identity_is_the_package_name()
    {
        Assert.Equal("Vestigium.Helpers.Hashing", HashingHelper.Identity);
        Assert.Equal(HashingHelper.Identity, HashingHelper.Probe());
    }

    [Fact]
    public void Sha256_empty_and_fips_abc()
    {
        Assert.Equal("e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855", HashingHelper.HashString(""));
        Assert.Equal("ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad", HashingHelper.HashString("abc"));
    }

    [Fact]
    public void Crc32_of_123456789_is_cbf43926()
    {
        Assert.Equal("cbf43926", HashingHelper.ChecksumCrc32("123456789"));
    }

    [Fact]
    public void Hmac_rfc4231_case1_sha256()
    {
        Span<byte> keyBytes = stackalloc byte[20];
        keyBytes.Fill(0x0b);
        using var key = HmacKey.FromBytes(keyBytes);
        Assert.Equal(
            "b0344c61d8db38535ca8afceaf0bf12b881dc200c9833da726e9376c2e32cff7",
            HashingHelper.HmacString("Hi There", key));
    }

    [Fact]
    public void File_over_64kib_matches_sha256_hashdata()
    {
        var payload = new byte[80_000];
        RandomNumberGenerator.Fill(payload);
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHashContract", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var path = Path.Combine(dir, "blob.bin");
        File.WriteAllBytes(path, payload);
        var expected = Convert.ToHexString(SHA256.HashData(payload)).ToLowerInvariant();
        Assert.Equal(expected, HashingHelper.HashFile(path));
    }

    [Fact]
    public void HashPassword_is_not_hashstring_and_verify_roundtrips()
    {
        const string password = "gallery-demo-only";
        var phc = HashingHelper.HashPassword(password);
        Assert.StartsWith("$argon2id$", phc);
        Assert.NotEqual(HashingHelper.HashString(password), phc);
        Assert.True(HashingHelper.VerifyPassword(password, phc));
        Assert.False(HashingHelper.VerifyPassword("gallery-demo-xxxx", phc));
        Assert.False(HashingHelper.VerifyPassword(password, "not-a-phc"));
    }

    [Fact]
    public void Md5_and_sha1_are_named_interop_only()
    {
        Assert.False(HashingHelper.IsInterop(HashingAlgorithm.Sha256));
        Assert.True(HashingHelper.IsInterop(HashingAlgorithm.Md5));
        Assert.True(HashingHelper.IsInterop(HashingAlgorithm.Sha1));
        Assert.Equal(32, HashingHelper.HashMd5("abc").Length);
        Assert.Equal(40, HashingHelper.HashSha1("abc").Length);
        Assert.Equal(64, HashingHelper.HashString("abc").Length);
        Assert.NotEqual(HashingHelper.HashMd5("abc"), HashingHelper.HashString("abc"));
    }

    [Fact]
    public void Hex_base64_of_abc_sha256()
    {
        var hex = HashingHelper.HashString("abc");
        var b64 = HashingConvert.HexToBase64(hex);
        Assert.Equal(hex, HashingConvert.Base64ToHex(b64));
        Assert.Equal(Convert.FromHexString(hex), HashingConvert.Parse(b64, HashingTextFormat.Base64));
    }

    [Fact]
    public void Sha3_kmac_shake_skip_when_unsupported()
    {
        if (HashingHelper.IsSupported(HashingAlgorithm.Sha3_256))
            Assert.Equal(64, HashingHelper.HashString("abc", HashingAlgorithm.Sha3_256).Length);

        if (HashingHelper.IsHmacSupported(HmacAlgorithm.Sha3_256))
        {
            using var key = HmacKey.Generate();
            Assert.Equal(64, HashingHelper.HmacString("Hi There", key, HmacAlgorithm.Sha3_256).Length);
        }

        if (HashingHelper.IsShakeSupported)
            Assert.Equal(64, HashingHelper.Shake128("abc").Length);

        if (HashingHelper.IsKmacSupported)
        {
            using var key = HmacKey.Generate();
            Assert.False(string.IsNullOrWhiteSpace(HashingHelper.Kmac128("abc", key)));
        }
    }
}
