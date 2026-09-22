using System.Security.Cryptography;
using System.Text;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Hashing;

public static partial class HashingHelper
{
    public const int KmacMinOutput = 16;
    public const int KmacMaxOutput = 1024;

    public static bool IsKmacSupported => System.Security.Cryptography.Kmac128.IsSupported;

    public static string KmacName(KmacAlgorithm algorithm) => algorithm switch
    {
        KmacAlgorithm.Kmac128 => "KMAC128",
        KmacAlgorithm.Kmac256 => "KMAC256",
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };

    public static int KmacDefaultLength(KmacAlgorithm algorithm) => algorithm switch
    {
        KmacAlgorithm.Kmac128 => 32,
        KmacAlgorithm.Kmac256 => 64,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };

    public static string Kmac128(string text, HmacKey key, HashingTextFormat format = HashingTextFormat.HexLower)
        => KmacString(text, key, KmacAlgorithm.Kmac128, format);

    public static string Kmac256(string text, HmacKey key, HashingTextFormat format = HashingTextFormat.HexLower)
        => KmacString(text, key, KmacAlgorithm.Kmac256, format);

    public static string KmacString(
        string text,
        HmacKey key,
        HashingTextFormat format = HashingTextFormat.HexLower)
        => KmacString(text, key, KmacAlgorithm.Kmac128, format);

    public static string KmacString(
        string text,
        HmacKey key,
        KmacAlgorithm algorithm,
        HashingTextFormat format = HashingTextFormat.HexLower)
        => KmacString(text, key, algorithm, KmacDefaultLength(algorithm), format);

    public static string KmacString(
        string text,
        HmacKey key,
        KmacAlgorithm algorithm,
        int outputLength,
        HashingTextFormat format = HashingTextFormat.HexLower,
        ReadOnlySpan<byte> customization = default)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentNullException.ThrowIfNull(key);
        var utf8 = Encoding.UTF8.GetBytes(text);
        try
        {
            using var scope = HashingLog.Begin("KmacString", $"alg={KmacName(algorithm)} bytes={utf8.Length} keyBytes={key.Length} out={outputLength}");
            HashingLog.Pending("KmacString", $"alg={KmacName(algorithm)} bytes={utf8.Length} keyBytes={key.Length} out={outputLength}");
            var mac = KmacData(utf8, key, algorithm, outputLength, customization);
            var printed = HashingConvert.Format(mac, format);
            HashingLog.Success("KmacString", $"alg={KmacName(algorithm)} bytes={utf8.Length} keyBytes={key.Length} out={outputLength}");
            return printed;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(utf8);
        }
    }

    public static string KmacBytes(
        ReadOnlySpan<byte> data,
        HmacKey key,
        HashingTextFormat format = HashingTextFormat.HexLower)
        => KmacBytes(data, key, KmacAlgorithm.Kmac128, format);

    public static string KmacBytes(
        ReadOnlySpan<byte> data,
        HmacKey key,
        KmacAlgorithm algorithm,
        HashingTextFormat format = HashingTextFormat.HexLower)
        => KmacBytes(data, key, algorithm, KmacDefaultLength(algorithm), format);

    public static string KmacBytes(
        ReadOnlySpan<byte> data,
        HmacKey key,
        KmacAlgorithm algorithm,
        int outputLength,
        HashingTextFormat format = HashingTextFormat.HexLower,
        ReadOnlySpan<byte> customization = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        using var scope = HashingLog.Begin("KmacBytes", $"alg={KmacName(algorithm)} bytes={data.Length} keyBytes={key.Length} out={outputLength}");
        HashingLog.Pending("KmacBytes", $"alg={KmacName(algorithm)} bytes={data.Length} keyBytes={key.Length} out={outputLength}");
        var printed = HashingConvert.Format(KmacData(data, key, algorithm, outputLength, customization), format);
        HashingLog.Success("KmacBytes", $"alg={KmacName(algorithm)} bytes={data.Length} keyBytes={key.Length} out={outputLength}");
        return printed;
    }

    public static byte[] KmacData(
        ReadOnlySpan<byte> data,
        HmacKey key,
        KmacAlgorithm algorithm = KmacAlgorithm.Kmac128,
        int outputLength = 0,
        ReadOnlySpan<byte> customization = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        EnsureKmacSupported();
        var length = outputLength == 0 ? KmacDefaultLength(algorithm) : outputLength;
        EnsureKmacOutput(length);
        return algorithm switch
        {
            KmacAlgorithm.Kmac128 => System.Security.Cryptography.Kmac128.HashData(key.Span, data, length, customization),
            KmacAlgorithm.Kmac256 => System.Security.Cryptography.Kmac256.HashData(key.Span, data, length, customization),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
        };
    }

    public static string KmacFile(
        string path,
        HmacKey key,
        HashingTextFormat format = HashingTextFormat.HexLower)
        => KmacFile(path, key, KmacAlgorithm.Kmac128, format);

    public static string KmacFile(
        string path,
        HmacKey key,
        KmacAlgorithm algorithm,
        HashingTextFormat format = HashingTextFormat.HexLower)
        => KmacFile(path, key, algorithm, KmacDefaultLength(algorithm), format);

    public static string KmacFile(
        string path,
        HmacKey key,
        KmacAlgorithm algorithm,
        int outputLength,
        HashingTextFormat format = HashingTextFormat.HexLower,
        ReadOnlySpan<byte> customization = default)
    {
        ArgumentNullException.ThrowIfNull(key);
        var file = HelperGuard.NotBlank(path, nameof(path));
        using var stream = OpenRead(file);
        return KmacFile(stream, key, algorithm, outputLength, format, customization, file);
    }

    public static string KmacFile(
        Stream stream,
        HmacKey key,
        KmacAlgorithm algorithm,
        int outputLength,
        HashingTextFormat format = HashingTextFormat.HexLower,
        ReadOnlySpan<byte> customization = default,
        string? pathName = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentNullException.ThrowIfNull(key);
        EnsureKmacSupported();
        var length = outputLength == 0 ? KmacDefaultLength(algorithm) : outputLength;
        EnsureKmacOutput(length);
        var visible = VisiblePath(pathName);
        using var scope = HashingLog.Begin("KmacFile", $"alg={KmacName(algorithm)} path={visible} keyBytes={key.Length} out={length}");
        HashingLog.Pending("KmacFile", $"alg={KmacName(algorithm)} path={visible} keyBytes={key.Length} out={length}");
        var mac = algorithm switch
        {
            KmacAlgorithm.Kmac128 => System.Security.Cryptography.Kmac128.HashData(key.Span, stream, length, customization),
            KmacAlgorithm.Kmac256 => System.Security.Cryptography.Kmac256.HashData(key.Span, stream, length, customization),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
        };
        var printed = HashingConvert.Format(mac, format);
        HashingLog.Success("KmacFile", $"alg={KmacName(algorithm)} path={visible} keyBytes={key.Length} out={length} digest={printed}");
        return printed;
    }

    public static bool VerifyKmacString(
        string text,
        HmacKey key,
        string expected,
        HashingTextFormat format = HashingTextFormat.HexLower)
        => VerifyKmacString(text, key, expected, KmacAlgorithm.Kmac128, format);

    public static bool VerifyKmacString(
        string text,
        HmacKey key,
        string expected,
        KmacAlgorithm algorithm,
        HashingTextFormat format = HashingTextFormat.HexLower,
        int outputLength = 0,
        ReadOnlySpan<byte> customization = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var length = outputLength == 0 ? KmacDefaultLength(algorithm) : outputLength;
        var actual = KmacData(Encoding.UTF8.GetBytes(text), key, algorithm, length, customization);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyKmacString", $"alg={KmacName(algorithm)} match={(ok ? "yes" : "no")}");
        return ok;
    }

    public static bool VerifyKmacFile(
        string path,
        HmacKey key,
        string expected,
        KmacAlgorithm algorithm = KmacAlgorithm.Kmac128,
        HashingTextFormat format = HashingTextFormat.HexLower,
        int outputLength = 0,
        ReadOnlySpan<byte> customization = default)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var length = outputLength == 0 ? KmacDefaultLength(algorithm) : outputLength;
        var actualHex = KmacFile(path, key, algorithm, length, HashingTextFormat.HexLower, customization);
        var actual = HashingConvert.Parse(actualHex, HashingTextFormat.HexLower);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyKmacFile", $"alg={KmacName(algorithm)} path={VisiblePath(path)} match={(ok ? "yes" : "no")}");
        return ok;
    }

    private static void EnsureKmacSupported()
    {
        if (!IsKmacSupported)
            throw new NotSupportedException("KMAC is not available on this OS.");
    }

    private static void EnsureKmacOutput(int outputLength)
    {
        if (outputLength < KmacMinOutput || outputLength > KmacMaxOutput)
            throw new ArgumentOutOfRangeException(nameof(outputLength), "KMAC output is 16–1024 bytes.");
    }
}
