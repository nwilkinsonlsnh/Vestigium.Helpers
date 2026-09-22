using System.IO.Hashing;
using System.Security.Cryptography;
using System.Text;

namespace Vestigium.Helpers.Hashing;

public static partial class HashingHelper
{
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

    public static string ChecksumString(string text, ChecksumAlgorithm algorithm = ChecksumAlgorithm.Crc32, HashingTextFormat format = HashingTextFormat.HexLower)
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

    public static string ChecksumBytes(ReadOnlySpan<byte> data, ChecksumAlgorithm algorithm = ChecksumAlgorithm.Crc32, HashingTextFormat format = HashingTextFormat.HexLower)
    {
        using var scope = HashingLog.Begin("ChecksumBytes", $"alg={ChecksumName(algorithm)} bytes={data.Length}");
        HashingLog.Pending("ChecksumBytes", $"alg={ChecksumName(algorithm)} bytes={data.Length}");
        var printed = HashingConvert.Format(ChecksumData(data, algorithm), format);
        HashingLog.Success("ChecksumBytes", $"alg={ChecksumName(algorithm)} bytes={data.Length}");
        return printed;
    }

    public static byte[] ChecksumData(ReadOnlySpan<byte> data, ChecksumAlgorithm algorithm = ChecksumAlgorithm.Crc32)
        => algorithm switch
        {
            ChecksumAlgorithm.Crc32 => U32Be(Crc32.HashToUInt32(data)),
            ChecksumAlgorithm.Crc64 => Crc64.Hash(data),
            ChecksumAlgorithm.XxHash32 => XxHash32.Hash(data),
            ChecksumAlgorithm.XxHash64 => XxHash64.Hash(data),
            ChecksumAlgorithm.XxHash3 => XxHash3.Hash(data),
            _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
        };

    public static string ChecksumFile(string path, ChecksumAlgorithm algorithm = ChecksumAlgorithm.Crc32, HashingTextFormat format = HashingTextFormat.HexLower)
    {
        var file = HelperGuard.NotBlank(path, nameof(path));
        using var stream = OpenRead(file);
        return ChecksumFile(stream, algorithm, format, file);
    }

    public static string ChecksumFile(Stream stream, ChecksumAlgorithm algorithm = ChecksumAlgorithm.Crc32, HashingTextFormat format = HashingTextFormat.HexLower, string? pathName = null)
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

    public static async Task<string> ChecksumFileAsync(Stream stream, ChecksumAlgorithm algorithm = ChecksumAlgorithm.Crc32, HashingTextFormat format = HashingTextFormat.HexLower, string? pathName = null, CancellationToken cancellationToken = default)
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

    public static bool VerifyChecksumString(string text, string expected, ChecksumAlgorithm algorithm = ChecksumAlgorithm.Crc32, HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentNullException.ThrowIfNull(text);
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var actual = ChecksumData(Encoding.UTF8.GetBytes(text), algorithm);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyChecksumString", $"alg={ChecksumName(algorithm)} match={(ok ? "yes" : "no")}");
        return ok;
    }

    public static bool VerifyChecksumFile(string path, string expected, ChecksumAlgorithm algorithm = ChecksumAlgorithm.Crc32, HashingTextFormat format = HashingTextFormat.HexLower)
    {
        ArgumentException.ThrowIfNullOrEmpty(expected);
        var actualHex = ChecksumFile(path, algorithm, HashingTextFormat.HexLower);
        var actual = HashingConvert.Parse(actualHex, HashingTextFormat.HexLower);
        var want = HashingConvert.Parse(expected, format);
        var ok = actual.Length == want.Length && CryptographicOperations.FixedTimeEquals(actual, want);
        HashingLog.Success("VerifyChecksumFile", $"alg={ChecksumName(algorithm)} path={VisiblePath(path)} match={(ok ? "yes" : "no")}");
        return ok;
    }
}
