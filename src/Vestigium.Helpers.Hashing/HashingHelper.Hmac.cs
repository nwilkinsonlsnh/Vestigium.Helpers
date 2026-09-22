using System.Security.Cryptography;
using System.Text;

namespace Vestigium.Helpers.Hashing;

public static partial class HashingHelper
{
    public static string HmacName(HmacAlgorithm algorithm) => algorithm switch
    {
        HmacAlgorithm.Sha256 => "HMAC-SHA256",
        HmacAlgorithm.Sha384 => "HMAC-SHA384",
        HmacAlgorithm.Sha512 => "HMAC-SHA512",
        HmacAlgorithm.Sha3_256 => "HMAC-SHA3-256",
        HmacAlgorithm.Sha3_384 => "HMAC-SHA3-384",
        HmacAlgorithm.Sha3_512 => "HMAC-SHA3-512",
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };

    public static int HmacLength(HmacAlgorithm algorithm) => algorithm switch
    {
        HmacAlgorithm.Sha256 or HmacAlgorithm.Sha3_256 => 32,
        HmacAlgorithm.Sha384 or HmacAlgorithm.Sha3_384 => 48,
        HmacAlgorithm.Sha512 or HmacAlgorithm.Sha3_512 => 64,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };

    public static bool IsHmacSupported(HmacAlgorithm algorithm) => algorithm switch
    {
        HmacAlgorithm.Sha256 or HmacAlgorithm.Sha384 or HmacAlgorithm.Sha512 => true,
        HmacAlgorithm.Sha3_256 => HMACSHA3_256.IsSupported,
        HmacAlgorithm.Sha3_384 => HMACSHA3_384.IsSupported,
        HmacAlgorithm.Sha3_512 => HMACSHA3_512.IsSupported,
        _ => false,
    };

    public static string HmacString(string text, HmacKey key, HashingTextFormat format = HashingTextFormat.HexLower)
        => HmacString(text, key, HmacAlgorithm.Sha256, format);

    public static string HmacString(string text, HmacKey key, HmacAlgorithm algorithm, HashingTextFormat format = HashingTextFormat.HexLower)
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

    public static string HmacBytes(ReadOnlySpan<byte> data, HmacKey key, HashingTextFormat format = HashingTextFormat.HexLower)
        => HmacBytes(data, key, HmacAlgorithm.Sha256, format);

    public static string HmacBytes(ReadOnlySpan<byte> data, HmacKey key, HmacAlgorithm algorithm, HashingTextFormat format = HashingTextFormat.HexLower)
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
        EnsureHmacSupported(algorithm);
        return algorithm switch
        {
            HmacAlgorithm.Sha256 => HMACSHA256.HashData(key.Span, data),
            HmacAlgorithm.Sha384 => HMACSHA384.HashData(key.Span, data),
            HmacAlgorithm.Sha512 => HMACSHA512.HashData(key.Span, data),
            HmacAlgorithm.Sha3_256 => HMACSHA3_256.HashData(key.Span, data),
            HmacAlgorithm.Sha3_384 => HMACSHA3_384.HashData(key.Span, data),
            HmacAlgorithm.Sha3_512 => HMACSHA3_512.HashData(key.Span, data),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
        };
    }

    public static string HmacFile(string path, HmacKey key, HashingTextFormat format = HashingTextFormat.HexLower)
        => HmacFile(path, key, HmacAlgorithm.Sha256, format);

    public static string HmacFile(string path, HmacKey key, HmacAlgorithm algorithm, HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentNullException.ThrowIfNull(key);
        var file = HelperGuard.NotBlank(path, nameof(path));
        using var stream = OpenRead(file);
        return HmacFile(stream, key, algorithm, format, file);
    }

    public static string HmacFile(Stream stream, HmacKey key, HashingTextFormat format = HashingTextFormat.HexLower, string? pathName = null)
        => HmacFile(stream, key, HmacAlgorithm.Sha256, format, pathName);

    public static string HmacFile(Stream stream, HmacKey key, HmacAlgorithm algorithm, HashingTextFormat format = HashingTextFormat.HexLower, string? pathName = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(key);
        EnsureHmacSupported(algorithm);
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

    public static bool VerifyHmacString(string text, HmacKey key, string expected, HashingTextFormat format = HashingTextFormat.HexLower)
        => VerifyHmacString(text, key, expected, HmacAlgorithm.Sha256, format);

    public static bool VerifyHmacString(string text, HmacKey key, string expected, HmacAlgorithm algorithm, HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var actual = HmacData(Encoding.UTF8.GetBytes(text), key, algorithm);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyHmacString", $"alg={HmacName(algorithm)} match={(ok ? "yes" : "no")}");
        return ok;
    }

    public static bool VerifyHmacFile(string path, HmacKey key, string expected, HashingTextFormat format = HashingTextFormat.HexLower)
        => VerifyHmacFile(path, key, expected, HmacAlgorithm.Sha256, format);

    public static bool VerifyHmacFile(string path, HmacKey key, string expected, HmacAlgorithm algorithm, HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var actualHex = HmacFile(path, key, algorithm, HashingTextFormat.HexLower);
        var actual = HashingConvert.Parse(actualHex, HashingTextFormat.HexLower);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyHmacFile", $"alg={HmacName(algorithm)} path={VisiblePath(path)} match={(ok ? "yes" : "no")}");
        return ok;
    }
}
