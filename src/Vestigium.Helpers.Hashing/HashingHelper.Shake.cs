using System.Security.Cryptography;
using System.Text;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Hashing;

public static partial class HashingHelper
{
    public const int ShakeMinOutput = 1;
    public const int ShakeMaxOutput = 1024;

    public static bool IsShakeSupported => System.Security.Cryptography.Shake128.IsSupported;

    public static string ShakeName(ShakeAlgorithm algorithm) => algorithm switch
    {
        ShakeAlgorithm.Shake128 => "SHAKE128",
        ShakeAlgorithm.Shake256 => "SHAKE256",
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };

    public static int ShakeDefaultLength(ShakeAlgorithm algorithm) => algorithm switch
    {
        ShakeAlgorithm.Shake128 => 32,
        ShakeAlgorithm.Shake256 => 64,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };

    public static string Shake128(string text, HashingTextFormat format = HashingTextFormat.HexLower)
        => ShakeString(text, ShakeAlgorithm.Shake128, format);

    public static string Shake256(string text, HashingTextFormat format = HashingTextFormat.HexLower)
        => ShakeString(text, ShakeAlgorithm.Shake256, format);

    public static string ShakeString(
        string text,
        HashingTextFormat format = HashingTextFormat.HexLower)
        => ShakeString(text, ShakeAlgorithm.Shake128, format);

    public static string ShakeString(
        string text,
        ShakeAlgorithm algorithm,
        HashingTextFormat format = HashingTextFormat.HexLower)
        => ShakeString(text, algorithm, ShakeDefaultLength(algorithm), format);

    public static string ShakeString(
        string text,
        ShakeAlgorithm algorithm,
        int outputLength,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentNullException.ThrowIfNull(text);
        var utf8 = Encoding.UTF8.GetBytes(text);
        try
        {
            using var scope = HashingLog.Begin("ShakeString", $"alg={ShakeName(algorithm)} bytes={utf8.Length} out={outputLength}");
            HashingLog.Pending("ShakeString", $"alg={ShakeName(algorithm)} bytes={utf8.Length} out={outputLength}");
            var digest = ShakeData(utf8, algorithm, outputLength);
            var printed = HashingConvert.Format(digest, format);
            HashingLog.Success("ShakeString", $"alg={ShakeName(algorithm)} bytes={utf8.Length} out={outputLength}");
            return printed;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(utf8);
        }
    }

    public static string ShakeBytes(
        ReadOnlySpan<byte> data,
        ShakeAlgorithm algorithm = ShakeAlgorithm.Shake128,
        HashingTextFormat format = HashingTextFormat.HexLower)
        => ShakeBytes(data, algorithm, ShakeDefaultLength(algorithm), format);

    public static string ShakeBytes(
        ReadOnlySpan<byte> data,
        ShakeAlgorithm algorithm,
        int outputLength,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        using var scope = HashingLog.Begin("ShakeBytes", $"alg={ShakeName(algorithm)} bytes={data.Length} out={outputLength}");
        HashingLog.Pending("ShakeBytes", $"alg={ShakeName(algorithm)} bytes={data.Length} out={outputLength}");
        var printed = HashingConvert.Format(ShakeData(data, algorithm, outputLength), format);
        HashingLog.Success("ShakeBytes", $"alg={ShakeName(algorithm)} bytes={data.Length} out={outputLength}");
        return printed;
    }

    public static byte[] ShakeData(
        ReadOnlySpan<byte> data,
        ShakeAlgorithm algorithm = ShakeAlgorithm.Shake128,
        int outputLength = 0)
    {
        EnsureShakeSupported();
        var length = outputLength == 0 ? ShakeDefaultLength(algorithm) : outputLength;
        EnsureShakeOutput(length);
        return algorithm switch
        {
            ShakeAlgorithm.Shake128 => System.Security.Cryptography.Shake128.HashData(data, length),
            ShakeAlgorithm.Shake256 => System.Security.Cryptography.Shake256.HashData(data, length),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
        };
    }

    public static string ShakeFile(
        string path,
        ShakeAlgorithm algorithm = ShakeAlgorithm.Shake128,
        HashingTextFormat format = HashingTextFormat.HexLower)
        => ShakeFile(path, algorithm, ShakeDefaultLength(algorithm), format);

    public static string ShakeFile(
        string path,
        ShakeAlgorithm algorithm,
        int outputLength,
        HashingTextFormat format = HashingTextFormat.HexLower)
    {
        var file = HelperGuard.NotBlank(path, nameof(path));
        using var stream = OpenRead(file);
        return ShakeFile(stream, algorithm, outputLength, format, file);
    }

    public static string ShakeFile(
        Stream stream,
        ShakeAlgorithm algorithm,
        int outputLength,
        HashingTextFormat format = HashingTextFormat.HexLower,
        string? pathName = null)
    {
        ArgumentNullException.ThrowIfNull(stream);
        EnsureShakeSupported();
        var length = outputLength == 0 ? ShakeDefaultLength(algorithm) : outputLength;
        EnsureShakeOutput(length);
        var visible = VisiblePath(pathName);
        using var scope = HashingLog.Begin("ShakeFile", $"alg={ShakeName(algorithm)} path={visible} out={length}");
        HashingLog.Pending("ShakeFile", $"alg={ShakeName(algorithm)} path={visible} out={length}");
        var digest = algorithm switch
        {
            ShakeAlgorithm.Shake128 => System.Security.Cryptography.Shake128.HashData(stream, length),
            ShakeAlgorithm.Shake256 => System.Security.Cryptography.Shake256.HashData(stream, length),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
        };
        var printed = HashingConvert.Format(digest, format);
        HashingLog.Success("ShakeFile", $"alg={ShakeName(algorithm)} path={visible} out={length} digest={printed}");
        return printed;
    }

    public static bool VerifyShakeString(
        string text,
        string expected,
        ShakeAlgorithm algorithm = ShakeAlgorithm.Shake128,
        HashingTextFormat format = HashingTextFormat.HexLower,
        int outputLength = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var length = outputLength == 0 ? ShakeDefaultLength(algorithm) : outputLength;
        var actual = ShakeData(Encoding.UTF8.GetBytes(text), algorithm, length);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyShakeString", $"alg={ShakeName(algorithm)} match={(ok ? "yes" : "no")}");
        return ok;
    }

    public static bool VerifyShakeFile(
        string path,
        string expected,
        ShakeAlgorithm algorithm = ShakeAlgorithm.Shake128,
        HashingTextFormat format = HashingTextFormat.HexLower,
        int outputLength = 0)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var length = outputLength == 0 ? ShakeDefaultLength(algorithm) : outputLength;
        var actualHex = ShakeFile(path, algorithm, length, HashingTextFormat.HexLower);
        var actual = HashingConvert.Parse(actualHex, HashingTextFormat.HexLower);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyShakeFile", $"alg={ShakeName(algorithm)} path={VisiblePath(path)} match={(ok ? "yes" : "no")}");
        return ok;
    }

    private static void EnsureShakeSupported()
    {
        if (!IsShakeSupported)
            throw new NotSupportedException("SHAKE is not available on this OS.");
    }

    private static void EnsureShakeOutput(int outputLength)
    {
        if (outputLength < ShakeMinOutput || outputLength > ShakeMaxOutput)
            throw new ArgumentOutOfRangeException(nameof(outputLength), "SHAKE output is 1–1024 bytes.");
    }
}
