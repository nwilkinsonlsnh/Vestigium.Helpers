using System.Buffers;
using System.Buffers.Binary;
using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Hashing;

/// <summary>
/// String and file hashing. SHA-256 default, SHA-384/512 and SHA-3 opt-in, HMAC-SHA256/384/512 keyed,
/// Argon2id PHC password verifiers, CRC-32 / CRC-64 / xxHash checksums. Not encryption.
/// </summary>
public static class HashingHelper
{
    public const int StreamBufferSize = 64 * 1024;

    public static string Identity => "Vestigium.Helpers.Hashing";

    public static string Probe()
    {
        HashingLog.Pending("Probe", "Opening a hashing helper probe.");
        var digest = HashString("abc");
        if (digest != "ba7816bf8f01cfea414140de5dae2223b00361a396177a9cb410ff61f20015ad")
            throw new CryptographicException("SHA-256 probe vector failed.");
        _ = HashingConvert.HexToBase64(digest);
        using var key = HmacKey.Generate();
        _ = HmacString("probe-message-16", key);
        Span<byte> rfc = stackalloc byte[20];
        rfc.Fill(0x0b);
        using var rfcKey = HmacKey.FromBytes(rfc);
        if (HmacString("Hi There", rfcKey, HmacAlgorithm.Sha384) !=
            "afd03944d84895626b0825f4ab46907f15f9dadbe4101ec682aa034c7cebc59cfaea9ea9076ede7f4af152e8b2fa9cb6")
            throw new CryptographicException("HMAC-SHA384 probe vector failed.");
        if (ChecksumCrc32("123456789") != "cbf43926")
            throw new CryptographicException("CRC-32 probe vector failed.");
        HashingLog.Success("Probe", "Hashing probe complete. Identity=" + Identity);
        return Identity;
    }

    public static string AlgorithmName(HashingAlgorithm algorithm) => algorithm switch
    {
        HashingAlgorithm.Sha256 => "SHA-256",
        HashingAlgorithm.Sha384 => "SHA-384",
        HashingAlgorithm.Sha512 => "SHA-512",
        HashingAlgorithm.Sha3_256 => "SHA3-256",
        HashingAlgorithm.Sha3_384 => "SHA3-384",
        HashingAlgorithm.Sha3_512 => "SHA3-512",
        HashingAlgorithm.Md5 => "MD5",
        HashingAlgorithm.Sha1 => "SHA-1",
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };

    public static int DigestLength(HashingAlgorithm algorithm) => algorithm switch
    {
        HashingAlgorithm.Sha256 or HashingAlgorithm.Sha3_256 => 32,
        HashingAlgorithm.Sha384 or HashingAlgorithm.Sha3_384 => 48,
        HashingAlgorithm.Sha512 or HashingAlgorithm.Sha3_512 => 64,
        HashingAlgorithm.Md5 => 16,
        HashingAlgorithm.Sha1 => 20,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };

    public static bool IsSupported(HashingAlgorithm algorithm) => algorithm switch
    {
        HashingAlgorithm.Sha256 or HashingAlgorithm.Sha384 or HashingAlgorithm.Sha512
            or HashingAlgorithm.Md5 or HashingAlgorithm.Sha1 => true,
        HashingAlgorithm.Sha3_256 => SHA3_256.IsSupported,
        HashingAlgorithm.Sha3_384 => SHA3_384.IsSupported,
        HashingAlgorithm.Sha3_512 => SHA3_512.IsSupported,
        _ => false,
    };

    public static bool IsInterop(HashingAlgorithm algorithm)
        => algorithm is HashingAlgorithm.Md5 or HashingAlgorithm.Sha1;

    public static string HashString(
        string text,
        HashingAlgorithm algorithm = HashingAlgorithm.Sha256,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentNullException.ThrowIfNull(text);
        var utf8 = Encoding.UTF8.GetBytes(text);
        try
        {
            using var scope = HashingLog.Begin("HashString", $"alg={AlgorithmName(algorithm)} bytes={utf8.Length}");
            HashingLog.Pending("HashString", $"alg={AlgorithmName(algorithm)} bytes={utf8.Length}");
            var digest = HashData(utf8, algorithm);
            var printed = HashingConvert.Format(digest, format);
            HashingLog.Success("HashString", $"alg={AlgorithmName(algorithm)} bytes={utf8.Length}");
            return printed;
        }
        catch (Exception ex) when (ex is not ArgumentException and not ArgumentOutOfRangeException and not NotSupportedException)
        {
            HashingLog.Failed("Hash failed.");
            throw;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(utf8);
        }
    }

    public static string HashBytes(
        ReadOnlySpan<byte> data,
        HashingAlgorithm algorithm = HashingAlgorithm.Sha256,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        using var scope = HashingLog.Begin("HashBytes", $"alg={AlgorithmName(algorithm)} bytes={data.Length}");
        HashingLog.Pending("HashBytes", $"alg={AlgorithmName(algorithm)} bytes={data.Length}");
        var digest = HashData(data, algorithm);
        var printed = HashingConvert.Format(digest, format);
        HashingLog.Success("HashBytes", $"alg={AlgorithmName(algorithm)} bytes={data.Length}");
        return printed;
    }

    public static byte[] HashData(ReadOnlySpan<byte> data, HashingAlgorithm algorithm = HashingAlgorithm.Sha256)
    {
        EnsureSupported(algorithm);
        var dest = new byte[DigestLength(algorithm)];
        HashInto(data, dest, algorithm);
        return dest;
    }

    public static bool TryHash(
        ReadOnlySpan<byte> source,
        Span<byte> destination,
        out int bytesWritten,
        HashingAlgorithm algorithm = HashingAlgorithm.Sha256)
    {
        EnsureSupported(algorithm);
        var size = DigestLength(algorithm);
        if (destination.Length < size)
        {
            bytesWritten = 0;
            return false;
        }

        HashInto(source, destination[..size], algorithm);
        bytesWritten = size;
        return true;
    }

    public static string HashMd5(string text, HashingTextFormat format = HashingTextFormat.HexLower)
        => HashString(text, HashingAlgorithm.Md5, format);

    public static string HashSha1(string text, HashingTextFormat format = HashingTextFormat.HexLower)
        => HashString(text, HashingAlgorithm.Sha1, format);

    public static string ChecksumName(ChecksumAlgorithm algorithm) => algorithm switch
    {
        ChecksumAlgorithm.Crc32 => "CRC-32",
        ChecksumAlgorithm.Crc64 => "CRC-64",
        ChecksumAlgorithm.XxHash32 => "XXH32",
        ChecksumAlgorithm.XxHash64 => "XXH64",
        ChecksumAlgorithm.XxHash3 => "XXH3",
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };

    public static int ChecksumLength(ChecksumAlgorithm algorithm) => algorithm switch
    {
        ChecksumAlgorithm.Crc32 or ChecksumAlgorithm.XxHash32 => 4,
        ChecksumAlgorithm.Crc64 or ChecksumAlgorithm.XxHash64 or ChecksumAlgorithm.XxHash3 => 8,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };

    public static string ChecksumCrc32(string text, HashingTextFormat format = HashingTextFormat.HexLower)
        => ChecksumString(text, ChecksumAlgorithm.Crc32, format);

    public static string ChecksumCrc64(string text, HashingTextFormat format = HashingTextFormat.HexLower)
        => ChecksumString(text, ChecksumAlgorithm.Crc64, format);

    public static string ChecksumXxHash(string text, HashingTextFormat format = HashingTextFormat.HexLower)
        => ChecksumString(text, ChecksumAlgorithm.XxHash64, format);

    public static string ChecksumString(
        string text,
        ChecksumAlgorithm algorithm = ChecksumAlgorithm.Crc32,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentNullException.ThrowIfNull(text);
        var utf8 = Encoding.UTF8.GetBytes(text);
        try
        {
            using var scope = HashingLog.Begin("ChecksumString", $"alg={ChecksumName(algorithm)} bytes={utf8.Length}");
            HashingLog.Pending("ChecksumString", $"alg={ChecksumName(algorithm)} bytes={utf8.Length}");
            var digest = ChecksumData(utf8, algorithm);
            var printed = HashingConvert.Format(digest, format);
            HashingLog.Success("ChecksumString", $"alg={ChecksumName(algorithm)} bytes={utf8.Length}");
            return printed;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(utf8);
        }
    }

    public static string ChecksumBytes(
        ReadOnlySpan<byte> data,
        ChecksumAlgorithm algorithm = ChecksumAlgorithm.Crc32,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        using var scope = HashingLog.Begin("ChecksumBytes", $"alg={ChecksumName(algorithm)} bytes={data.Length}");
        HashingLog.Pending("ChecksumBytes", $"alg={ChecksumName(algorithm)} bytes={data.Length}");
        var printed = HashingConvert.Format(ChecksumData(data, algorithm), format);
        HashingLog.Success("ChecksumBytes", $"alg={ChecksumName(algorithm)} bytes={data.Length}");
        return printed;
    }

    public static byte[] ChecksumData(ReadOnlySpan<byte> data, ChecksumAlgorithm algorithm = ChecksumAlgorithm.Crc32)
    {
        return algorithm switch
        {
            ChecksumAlgorithm.Crc32 => U32Be(Crc32.HashToUInt32(data)),
            ChecksumAlgorithm.Crc64 => Crc64.Hash(data),
            ChecksumAlgorithm.XxHash32 => XxHash32.Hash(data),
            ChecksumAlgorithm.XxHash64 => XxHash64.Hash(data),
            ChecksumAlgorithm.XxHash3 => XxHash3.Hash(data),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
        };
    }

    public static string ChecksumFile(
        string path,
        ChecksumAlgorithm algorithm = ChecksumAlgorithm.Crc32,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        var file = HelperGuard.NotBlank(path, nameof(path));
        using var stream = OpenRead(file);
        return ChecksumFile(stream, algorithm, format, file);
    }

    public static string ChecksumFile(
        Stream stream,
        ChecksumAlgorithm algorithm = ChecksumAlgorithm.Crc32,
        HashingTextFormat format = HashingTextFormat.HexLower,
        string? pathName = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var visible = VisiblePath(pathName);
        using var scope = HashingLog.Begin("ChecksumFile", $"alg={ChecksumName(algorithm)} path={visible}");
        HashingLog.Pending("ChecksumFile", $"alg={ChecksumName(algorithm)} path={visible}");
        var digest = ChecksumStream(stream, algorithm, out var bytes);
        var printed = HashingConvert.Format(digest, format);
        HashingLog.Success("ChecksumFile", $"alg={ChecksumName(algorithm)} path={visible} bytes={bytes} digest={printed}");
        return printed;
    }

    public static async Task<string> ChecksumFileAsync(
        Stream stream,
        ChecksumAlgorithm algorithm = ChecksumAlgorithm.Crc32,
        HashingTextFormat format = HashingTextFormat.HexLower,
        string? pathName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        var visible = VisiblePath(pathName);
        using var scope = HashingLog.Begin("ChecksumFileAsync", $"alg={ChecksumName(algorithm)} path={visible}");
        HashingLog.Pending("ChecksumFileAsync", $"alg={ChecksumName(algorithm)} path={visible}");
        var (digest, bytes) = await ChecksumStreamAsync(stream, algorithm, cancellationToken).ConfigureAwait(false);
        var printed = HashingConvert.Format(digest, format);
        HashingLog.Success("ChecksumFileAsync", $"alg={ChecksumName(algorithm)} path={visible} bytes={bytes} digest={printed}");
        return printed;
    }

    public static bool VerifyChecksumString(
        string text,
        string expected,
        ChecksumAlgorithm algorithm = ChecksumAlgorithm.Crc32,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var actual = ChecksumData(Encoding.UTF8.GetBytes(text), algorithm);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyChecksumString", $"alg={ChecksumName(algorithm)} match={(ok ? "yes" : "no")}");
        return ok;
    }

    public static bool VerifyChecksumFile(
        string path,
        string expected,
        ChecksumAlgorithm algorithm = ChecksumAlgorithm.Crc32,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var actualHex = ChecksumFile(path, algorithm, HashingTextFormat.HexLower);
        var actual = HashingConvert.Parse(actualHex, HashingTextFormat.HexLower);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyChecksumFile", $"alg={ChecksumName(algorithm)} path={VisiblePath(path)} match={(ok ? "yes" : "no")}");
        return ok;
    }

    public static string HashFile(
        string path,
        HashingAlgorithm algorithm = HashingAlgorithm.Sha256,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        var file = HelperGuard.NotBlank(path, nameof(path));
        using var stream = OpenRead(file);
        return HashFile(stream, algorithm, format, file);
    }

    public static string HashFile(
        Stream stream,
        HashingAlgorithm algorithm = HashingAlgorithm.Sha256,
        HashingTextFormat format = HashingTextFormat.HexLower,
        string? pathName = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        EnsureSupported(algorithm);
        var visible = VisiblePath(pathName);
        using var scope = HashingLog.Begin("HashFile", $"alg={AlgorithmName(algorithm)} path={visible}");
        HashingLog.Pending("HashFile", $"alg={AlgorithmName(algorithm)} path={visible}");
        var digest = HashStream(stream, algorithm, out var bytes);
        var printed = HashingConvert.Format(digest, format);
        HashingLog.Success("HashFile", $"alg={AlgorithmName(algorithm)} path={visible} bytes={bytes} digest={printed}");
        return printed;
    }

    public static async Task<string> HashFileAsync(
        Stream stream,
        HashingAlgorithm algorithm = HashingAlgorithm.Sha256,
        HashingTextFormat format = HashingTextFormat.HexLower,
        string? pathName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(stream);
        EnsureSupported(algorithm);
        var visible = VisiblePath(pathName);
        using var scope = HashingLog.Begin("HashFileAsync", $"alg={AlgorithmName(algorithm)} path={visible}");
        HashingLog.Pending("HashFileAsync", $"alg={AlgorithmName(algorithm)} path={visible}");
        var (digest, bytes) = await HashStreamAsync(stream, algorithm, cancellationToken).ConfigureAwait(false);
        var printed = HashingConvert.Format(digest, format);
        HashingLog.Success("HashFileAsync", $"alg={AlgorithmName(algorithm)} path={visible} bytes={bytes} digest={printed}");
        return printed;
    }

    public static bool VerifyString(
        string text,
        string expected,
        HashingAlgorithm algorithm = HashingAlgorithm.Sha256,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var actual = HashData(Encoding.UTF8.GetBytes(text), algorithm);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyString", $"alg={AlgorithmName(algorithm)} match={(ok ? "yes" : "no")}");
        return ok;
    }

    public static bool VerifyBytes(
        ReadOnlySpan<byte> data,
        string expected,
        HashingAlgorithm algorithm = HashingAlgorithm.Sha256,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var actual = HashData(data, algorithm);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyBytes", $"alg={AlgorithmName(algorithm)} match={(ok ? "yes" : "no")}");
        return ok;
    }

    public static bool VerifyFile(
        string path,
        string expected,
        HashingAlgorithm algorithm = HashingAlgorithm.Sha256,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var actualHex = HashFile(path, algorithm, HashingTextFormat.HexLower);
        var actual = HashingConvert.Parse(actualHex, HashingTextFormat.HexLower);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyFile", $"alg={AlgorithmName(algorithm)} path={VisiblePath(path)} match={(ok ? "yes" : "no")}");
        return ok;
    }

    public static string HmacName(HmacAlgorithm algorithm) => algorithm switch
    {
        HmacAlgorithm.Sha256 => "HMAC-SHA256",
        HmacAlgorithm.Sha384 => "HMAC-SHA384",
        HmacAlgorithm.Sha512 => "HMAC-SHA512",
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };

    public static int HmacLength(HmacAlgorithm algorithm) => algorithm switch
    {
        HmacAlgorithm.Sha256 => 32,
        HmacAlgorithm.Sha384 => 48,
        HmacAlgorithm.Sha512 => 64,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };

    public static string HmacString(
        string text,
        HmacKey key,
        HashingTextFormat format = HashingTextFormat.HexLower)
        => HmacString(text, key, HmacAlgorithm.Sha256, format);

    public static string HmacString(
        string text,
        HmacKey key,
        HmacAlgorithm algorithm,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(key);
        var utf8 = Encoding.UTF8.GetBytes(text);
        try
        {
            using var scope = HashingLog.Begin("HmacString", $"alg={HmacName(algorithm)} bytes={utf8.Length} keyBytes={key.Length}");
            HashingLog.Pending("HmacString", $"alg={HmacName(algorithm)} bytes={utf8.Length} keyBytes={key.Length}");
            var mac = HmacData(utf8, key, algorithm);
            var printed = HashingConvert.Format(mac, format);
            HashingLog.Success("HmacString", $"alg={HmacName(algorithm)} bytes={utf8.Length} keyBytes={key.Length}");
            return printed;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(utf8);
        }
    }

    public static string HmacBytes(
        ReadOnlySpan<byte> data,
        HmacKey key,
        HashingTextFormat format = HashingTextFormat.HexLower)
        => HmacBytes(data, key, HmacAlgorithm.Sha256, format);

    public static string HmacBytes(
        ReadOnlySpan<byte> data,
        HmacKey key,
        HmacAlgorithm algorithm,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentNullException.ThrowIfNull(key);
        using var scope = HashingLog.Begin("HmacBytes", $"alg={HmacName(algorithm)} bytes={data.Length} keyBytes={key.Length}");
        HashingLog.Pending("HmacBytes", $"alg={HmacName(algorithm)} bytes={data.Length} keyBytes={key.Length}");
        var printed = HashingConvert.Format(HmacData(data, key, algorithm), format);
        HashingLog.Success("HmacBytes", $"alg={HmacName(algorithm)} bytes={data.Length} keyBytes={key.Length}");
        return printed;
    }

    public static byte[] HmacData(ReadOnlySpan<byte> data, HmacKey key, HmacAlgorithm algorithm = HmacAlgorithm.Sha256)
    {
        ArgumentNullException.ThrowIfNull(key);
        return algorithm switch
        {
            HmacAlgorithm.Sha256 => HMACSHA256.HashData(key.Span, data),
            HmacAlgorithm.Sha384 => HMACSHA384.HashData(key.Span, data),
            HmacAlgorithm.Sha512 => HMACSHA512.HashData(key.Span, data),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
        };
    }

    public static string HmacFile(
        string path,
        HmacKey key,
        HashingTextFormat format = HashingTextFormat.HexLower)
        => HmacFile(path, key, HmacAlgorithm.Sha256, format);

    public static string HmacFile(
        string path,
        HmacKey key,
        HmacAlgorithm algorithm,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentNullException.ThrowIfNull(key);
        var file = HelperGuard.NotBlank(path, nameof(path));
        using var stream = OpenRead(file);
        return HmacFile(stream, key, algorithm, format, file);
    }

    public static string HmacFile(
        Stream stream,
        HmacKey key,
        HashingTextFormat format = HashingTextFormat.HexLower,
        string? pathName = null)
        => HmacFile(stream, key, HmacAlgorithm.Sha256, format, pathName);

    public static string HmacFile(
        Stream stream,
        HmacKey key,
        HmacAlgorithm algorithm,
        HashingTextFormat format = HashingTextFormat.HexLower,
        string? pathName = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(key);
        var visible = VisiblePath(pathName);
        using var scope = HashingLog.Begin("HmacFile", $"alg={HmacName(algorithm)} path={visible} keyBytes={key.Length}");
        HashingLog.Pending("HmacFile", $"alg={HmacName(algorithm)} path={visible} keyBytes={key.Length}");
        using var hmac = IncrementalHash.CreateHMAC(HmacAlgName(algorithm), key.Span);
        var bytes = Pump(stream, hmac);
        var mac = hmac.GetHashAndReset();
        var printed = HashingConvert.Format(mac, format);
        HashingLog.Success("HmacFile", $"alg={HmacName(algorithm)} path={visible} bytes={bytes} keyBytes={key.Length} digest={printed}");
        return printed;
    }

    public static bool VerifyHmacString(
        string text,
        HmacKey key,
        string expected,
        HashingTextFormat format = HashingTextFormat.HexLower)
        => VerifyHmacString(text, key, expected, HmacAlgorithm.Sha256, format);

    public static bool VerifyHmacString(
        string text,
        HmacKey key,
        string expected,
        HmacAlgorithm algorithm,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var actual = HmacData(Encoding.UTF8.GetBytes(text), key, algorithm);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyHmacString", $"alg={HmacName(algorithm)} match={(ok ? "yes" : "no")}");
        return ok;
    }

    public static bool VerifyHmacFile(
        string path,
        HmacKey key,
        string expected,
        HashingTextFormat format = HashingTextFormat.HexLower)
        => VerifyHmacFile(path, key, expected, HmacAlgorithm.Sha256, format);

    public static bool VerifyHmacFile(
        string path,
        HmacKey key,
        string expected,
        HmacAlgorithm algorithm,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var actualHex = HmacFile(path, key, algorithm, HashingTextFormat.HexLower);
        var actual = HashingConvert.Parse(actualHex, HashingTextFormat.HexLower);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyHmacFile", $"alg={HmacName(algorithm)} path={VisiblePath(path)} match={(ok ? "yes" : "no")}");
        return ok;
    }

    public static string HashPassword(string password)
    {
        using var scope = HashingLog.Begin("HashPassword", "argon2id");
        HashingLog.Pending("HashPassword", "argon2id m=19456 t=2 p=1");
        try
        {
            var stored = PasswordHash.Hash(password);
            HashingLog.Success("HashPassword", "password hashed");
            return stored;
        }
        catch (Exception ex) when (ex is ArgumentException)
        {
            HashingLog.Failed("Password hash rejected.");
            throw;
        }
    }

    public static bool VerifyPassword(string password, string stored)
    {
        using var scope = HashingLog.Begin("VerifyPassword", "argon2id");
        HashingLog.Pending("VerifyPassword", "argon2id");
        try
        {
            var ok = PasswordHash.Verify(password, stored);
            HashingLog.Success("VerifyPassword", ok ? "password verify ok" : "password verify failed");
            return ok;
        }
        catch (Exception ex) when (ex is FormatException or CryptographicException or ArgumentException)
        {
            HashingLog.Failed("Password verify rejected.");
            throw;
        }
    }

#pragma warning disable CA5350, CA5351
    private static void HashInto(ReadOnlySpan<byte> data, Span<byte> destination, HashingAlgorithm algorithm)
    {
        switch (algorithm)
        {
            case HashingAlgorithm.Sha256:
                SHA256.HashData(data, destination);
                break;
            case HashingAlgorithm.Sha384:
                SHA384.HashData(data, destination);
                break;
            case HashingAlgorithm.Sha512:
                SHA512.HashData(data, destination);
                break;
            case HashingAlgorithm.Sha3_256:
                SHA3_256.HashData(data, destination);
                break;
            case HashingAlgorithm.Sha3_384:
                SHA3_384.HashData(data, destination);
                break;
            case HashingAlgorithm.Sha3_512:
                SHA3_512.HashData(data, destination);
                break;
            case HashingAlgorithm.Md5:
                MD5.HashData(data, destination);
                break;
            case HashingAlgorithm.Sha1:
                SHA1.HashData(data, destination);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(algorithm));
        }
    }

    private static HashAlgorithmName HmacAlgName(HmacAlgorithm algorithm) => algorithm switch
    {
        HmacAlgorithm.Sha256 => HashAlgorithmName.SHA256,
        HmacAlgorithm.Sha384 => HashAlgorithmName.SHA384,
        HmacAlgorithm.Sha512 => HashAlgorithmName.SHA512,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };

    private static HashAlgorithmName AlgName(HashingAlgorithm algorithm) => algorithm switch
    {
        HashingAlgorithm.Sha256 => HashAlgorithmName.SHA256,
        HashingAlgorithm.Sha384 => HashAlgorithmName.SHA384,
        HashingAlgorithm.Sha512 => HashAlgorithmName.SHA512,
        HashingAlgorithm.Sha3_256 => HashAlgorithmName.SHA3_256,
        HashingAlgorithm.Sha3_384 => HashAlgorithmName.SHA3_384,
        HashingAlgorithm.Sha3_512 => HashAlgorithmName.SHA3_512,
        HashingAlgorithm.Md5 => HashAlgorithmName.MD5,
        HashingAlgorithm.Sha1 => HashAlgorithmName.SHA1,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };
#pragma warning restore CA5350, CA5351

    private static byte[] HashStream(Stream stream, HashingAlgorithm algorithm, out long bytes)
    {
        using var hasher = IncrementalHash.CreateHash(AlgName(algorithm));
        bytes = Pump(stream, hasher);
        return hasher.GetHashAndReset();
    }

    private static async Task<(byte[] digest, long bytes)> HashStreamAsync(
        Stream stream,
        HashingAlgorithm algorithm,
        CancellationToken cancellationToken)
    {
        using var hasher = IncrementalHash.CreateHash(AlgName(algorithm));
        var buffer = ArrayPool<byte>.Shared.Rent(StreamBufferSize);
        long total = 0;
        try
        {
            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, StreamBufferSize), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    break;
                hasher.AppendData(buffer.AsSpan(0, read));
                total += read;
            }

            return (hasher.GetHashAndReset(), total);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    private static long Pump(Stream stream, IncrementalHash hasher)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(StreamBufferSize);
        long total = 0;
        try
        {
            int read;
            while ((read = stream.Read(buffer, 0, StreamBufferSize)) > 0)
            {
                hasher.AppendData(buffer.AsSpan(0, read));
                total += read;
            }

            return total;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    private static NonCryptographicHashAlgorithm CreateChecksum(ChecksumAlgorithm algorithm) => algorithm switch
    {
        ChecksumAlgorithm.Crc32 => new Crc32(),
        ChecksumAlgorithm.Crc64 => new Crc64(),
        ChecksumAlgorithm.XxHash32 => new XxHash32(),
        ChecksumAlgorithm.XxHash64 => new XxHash64(),
        ChecksumAlgorithm.XxHash3 => new XxHash3(),
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };

    private static byte[] U32Be(uint value)
    {
        var dest = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(dest, value);
        return dest;
    }

    private static byte[] ReverseCopy(byte[] source)
    {
        var dest = new byte[source.Length];
        for (var i = 0; i < source.Length; i++)
            dest[i] = source[source.Length - 1 - i];
        return dest;
    }

    private static byte[] ChecksumStream(Stream stream, ChecksumAlgorithm algorithm, out long bytes)
    {
        using var hasher = CreateChecksum(algorithm);
        bytes = PumpChecksum(stream, hasher);
        var digest = hasher.GetHashAndReset();
        return algorithm is ChecksumAlgorithm.Crc32 ? ReverseCopy(digest) : digest;
    }

    private static async Task<(byte[] digest, long bytes)> ChecksumStreamAsync(
        Stream stream,
        ChecksumAlgorithm algorithm,
        CancellationToken cancellationToken)
    {
        using var hasher = CreateChecksum(algorithm);
        var buffer = ArrayPool<byte>.Shared.Rent(StreamBufferSize);
        long total = 0;
        try
        {
            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, StreamBufferSize), cancellationToken).ConfigureAwait(false);
                if (read == 0)
                    break;
                hasher.Append(buffer.AsSpan(0, read));
                total += read;
            }

            var digest = hasher.GetHashAndReset();
            return (algorithm is ChecksumAlgorithm.Crc32 ? ReverseCopy(digest) : digest, total);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    private static long PumpChecksum(Stream stream, NonCryptographicHashAlgorithm hasher)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(StreamBufferSize);
        long total = 0;
        try
        {
            int read;
            while ((read = stream.Read(buffer, 0, StreamBufferSize)) > 0)
            {
                hasher.Append(buffer.AsSpan(0, read));
                total += read;
            }

            return total;
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }
    }

    private static FileStream OpenRead(string path)
        => new(path, FileMode.Open, FileAccess.Read, FileShare.Read, StreamBufferSize, FileOptions.SequentialScan);

    private static void EnsureSupported(HashingAlgorithm algorithm)
    {
        if (!IsSupported(algorithm))
            throw new NotSupportedException($"{AlgorithmName(algorithm)} is not available on this OS.");
    }

    private static string VisiblePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "(stream)";
        try
        {
            return Path.GetFileName(path);
        }
        catch (ArgumentException)
        {
            return "(path)";
        }
    }
}
