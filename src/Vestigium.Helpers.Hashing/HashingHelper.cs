using System.Buffers;
using System.Security.Cryptography;
using System.Text;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Hashing;

/// <summary>
/// String and file hashing. SHA-256 default, SHA-384/512 and SHA-3 opt-in, HMAC-SHA256 keyed,
/// Argon2id PHC password verifiers. Not encryption. Libraries never call Initialize.
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

    public static string HmacString(
        string text,
        HmacKey key,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(key);
        var utf8 = Encoding.UTF8.GetBytes(text);
        try
        {
            using var scope = HashingLog.Begin("HmacString", $"bytes={utf8.Length} keyBytes={key.Length}");
            HashingLog.Pending("HmacString", $"bytes={utf8.Length} keyBytes={key.Length}");
            var mac = HmacData(utf8, key);
            var printed = HashingConvert.Format(mac, format);
            HashingLog.Success("HmacString", $"bytes={utf8.Length} keyBytes={key.Length}");
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
    {
        ArgumentNullException.ThrowIfNull(key);
        using var scope = HashingLog.Begin("HmacBytes", $"bytes={data.Length} keyBytes={key.Length}");
        HashingLog.Pending("HmacBytes", $"bytes={data.Length} keyBytes={key.Length}");
        var mac = HmacData(data, key);
        var printed = HashingConvert.Format(mac, format);
        HashingLog.Success("HmacBytes", $"bytes={data.Length} keyBytes={key.Length}");
        return printed;
    }

    public static byte[] HmacData(ReadOnlySpan<byte> data, HmacKey key)
    {
        ArgumentNullException.ThrowIfNull(key);
        return HMACSHA256.HashData(key.Span, data);
    }

    public static string HmacFile(
        string path,
        HmacKey key,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentNullException.ThrowIfNull(key);
        var file = HelperGuard.NotBlank(path, nameof(path));
        using var stream = OpenRead(file);
        return HmacFile(stream, key, format, file);
    }

    public static string HmacFile(
        Stream stream,
        HmacKey key,
        HashingTextFormat format = HashingTextFormat.HexLower,
        string? pathName = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(key);
        var visible = VisiblePath(pathName);
        using var scope = HashingLog.Begin("HmacFile", $"path={visible} keyBytes={key.Length}");
        HashingLog.Pending("HmacFile", $"path={visible} keyBytes={key.Length}");
        using var hmac = IncrementalHash.CreateHMAC(HashAlgorithmName.SHA256, key.Span);
        var bytes = Pump(stream, hmac);
        var mac = hmac.GetHashAndReset();
        var printed = HashingConvert.Format(mac, format);
        HashingLog.Success("HmacFile", $"path={visible} bytes={bytes} keyBytes={key.Length} digest={printed}");
        return printed;
    }

    public static bool VerifyHmacString(string text, HmacKey key, string expected, HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var actual = HmacData(Encoding.UTF8.GetBytes(text), key);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyHmacString", $"match={(ok ? "yes" : "no")}");
        return ok;
    }

    public static bool VerifyHmacFile(string path, HmacKey key, string expected, HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var actualHex = HmacFile(path, key, HashingTextFormat.HexLower);
        var actual = HashingConvert.Parse(actualHex, HashingTextFormat.HexLower);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyHmacFile", $"path={VisiblePath(path)} match={(ok ? "yes" : "no")}");
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
