using System.Security.Cryptography;
using Vestigium.Helpers.Hashing;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class HashingPR01Tests
{
    public HashingPR01Tests() => VestigiumLogger.Shutdown();

    private static string Init()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHashPR01", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        VestigiumLogger.Initialize(cfg =>
        {
            cfg.AppId = HashingCatalog.AppId;
            cfg.LogDirectory = dir;
            cfg.MinimumDiskLevel = VestigiumLogLevel.Debug;
            cfg.OperationsLogEnabled = false;
            HashingCatalog.Register(cfg);
        });
        return dir;
    }

    [Fact]
    public void VerifyPassword_garbage_phc_returns_false()
    {
        try
        {
            Init();
            const string pwd = "gallery-demo-only";
            Assert.False(HashingHelper.VerifyPassword(pwd, "not-a-phc"));
            Assert.False(HashingHelper.VerifyPassword(pwd, ""));
            Assert.False(HashingHelper.VerifyPassword(pwd, "$argon2i$v=19$m=19456,t=2,p=1$c2FsdHNhbHQ$c2FsdHNhbHRzYWx0c2FsdHNhbHRzYWx0c2FsdA"));
            Assert.False(HashingHelper.VerifyPassword(pwd, "$argon2id$v=16$m=19456,t=2,p=1$c2FsdHNhbHQ$c2FsdHNhbHRzYWx0c2FsdHNhbHRzYWx0c2FsdA"));
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("\"EVENTID\":13020"));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void VerifyPassword_parameter_bomb_throws()
    {
        var salt8 = Convert.ToBase64String(new byte[8]).TrimEnd('=');
        var hash32 = Convert.ToBase64String(new byte[32]).TrimEnd('=');
        Assert.Throws<CryptographicException>(() => HashingHelper.VerifyPassword("correct horse", $"$argon2id$v=19$m=65537,t=2,p=1${salt8}${hash32}"));
        Assert.Throws<CryptographicException>(() => HashingHelper.VerifyPassword("correct horse", $"$argon2id$v=19$m=19456,t=11,p=1${salt8}${hash32}"));
        Assert.Throws<CryptographicException>(() => HashingHelper.VerifyPassword("correct horse", $"$argon2id$v=19$m=19456,t=2,p=5${salt8}${hash32}"));
        Assert.Throws<CryptographicException>(() => HashingHelper.VerifyPassword("correct horse", $"$argon2id$v=19$m=7,t=2,p=1${salt8}${hash32}"));
    }

    [Fact]
    public void HashPassword_length_still_throws()
    {
        Assert.Throws<ArgumentException>(() => HashingHelper.HashPassword(""));
        Assert.Throws<ArgumentException>(() => HashingHelper.HashPassword(new string('x', 129)));
        var salt8 = Convert.ToBase64String(new byte[8]).TrimEnd('=');
        var hash32 = Convert.ToBase64String(new byte[32]).TrimEnd('=');
        Assert.Throws<ArgumentException>(() => HashingHelper.VerifyPassword("short", $"$argon2id$v=19$m=19456,t=2,p=1${salt8}${hash32}"));
    }

    [Fact]
    public void HmacKey_ToString_is_not_hex()
    {
        using var key = HmacKey.Generate();
        var hex = key.ToHexLower();
        var printed = key.ToString();
        Assert.Equal("HmacKey(32 bytes)", printed);
        Assert.DoesNotContain(hex, printed);
        Assert.DoesNotContain(key.ToBase64(), printed);
        key.Dispose();
        Assert.Equal("HmacKey(disposed)", key.ToString());
    }

    [Fact]
    public void Hmac_jsonl_has_keyBytes_not_key_hex()
    {
        try
        {
            Init();
            var material = new byte[32];
            for (var i = 0; i < material.Length; i++)
                material[i] = (byte)(0xA0 + i);
            using var key = HmacKey.FromBytes(material);
            var keyHex = key.ToHexLower();
            var keyB64 = key.ToBase64();
            _ = HashingHelper.HmacString("gallery-demo-only", key);
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("keyBytes=32"));
            Assert.DoesNotContain(VestigiumLogger.RecentJsonLines, line => line.Contains(keyHex, StringComparison.OrdinalIgnoreCase));
            Assert.DoesNotContain(VestigiumLogger.RecentJsonLines, line => line.Contains(keyB64));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }

    [Fact]
    public void HashFile_jsonl_may_contain_digest_hex()
    {
        try
        {
            Init();
            var dir = Path.Combine(Path.GetTempPath(), "VestigiumHashPR01", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            var path = Path.Combine(dir, "payload.bin");
            File.WriteAllText(path, "abc");
            var digest = HashingHelper.HashFile(path);
            VestigiumLogger.Flush();
            Assert.Contains(VestigiumLogger.RecentJsonLines, line => line.Contains("digest=" + digest));
        }
        finally
        {
            VestigiumLogger.Shutdown();
        }
    }
}
