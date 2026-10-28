using System.Security.Cryptography;
using System.Text;

namespace Vestigium.Helpers.Hashing;

public static partial class HashingHelper
{
    public static string HashFile(string path, HashingAlgorithm algorithm = HashingAlgorithm.Sha256, HashingTextFormat format = HashingTextFormat.HexLower)
    {
        var file = HashingLog.RequireNotBlank(path, nameof(path));
        using var stream = OpenRead(file);
        return HashFile(stream, algorithm, format, file);
    }

    public static string HashFile(Stream stream, HashingAlgorithm algorithm = HashingAlgorithm.Sha256, HashingTextFormat format = HashingTextFormat.HexLower, string? pathName = null)
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

    public static async Task<string> HashFileAsync(Stream stream, HashingAlgorithm algorithm = HashingAlgorithm.Sha256, HashingTextFormat format = HashingTextFormat.HexLower, string? pathName = null, CancellationToken cancellationToken = default)
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

    public static bool VerifyString(string text, string expected, HashingAlgorithm algorithm = HashingAlgorithm.Sha256, HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var actual = HashData(Encoding.UTF8.GetBytes(text), algorithm);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyString", $"alg={AlgorithmName(algorithm)} match={(ok ? "yes" : "no")}");
        return ok;
    }

    public static bool VerifyBytes(ReadOnlySpan<byte> data, string expected, HashingAlgorithm algorithm = HashingAlgorithm.Sha256, HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var actual = HashData(data, algorithm);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyBytes", $"alg={AlgorithmName(algorithm)} match={(ok ? "yes" : "no")}");
        return ok;
    }

    public static bool VerifyFile(string path, string expected, HashingAlgorithm algorithm = HashingAlgorithm.Sha256, HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var actualHex = HashFile(path, algorithm, HashingTextFormat.HexLower);
        var actual = HashingConvert.Parse(actualHex, HashingTextFormat.HexLower);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyFile", $"alg={AlgorithmName(algorithm)} path={VisiblePath(path)} match={(ok ? "yes" : "no")}");
        return ok;
    }
}
