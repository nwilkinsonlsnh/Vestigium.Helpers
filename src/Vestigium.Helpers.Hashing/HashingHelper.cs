using System.Security.Cryptography;
using System.Text;

namespace Vestigium.Helpers.Hashing;

/// <summary>
/// String and file hashing. SHA-256 default. Not encryption.
/// </summary>
public static partial class HashingHelper
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
        if (HMACSHA3_256.IsSupported &&
            HmacString("Hi There", rfcKey, HmacAlgorithm.Sha3256) !=
            "ba85192310dffa96e2a3a40e69774351140bb7185e1202cdcc917589f95e16bb")
            throw new CryptographicException("HMAC-SHA3-256 probe vector failed.");
        if (System.Security.Cryptography.Shake128.IsSupported &&
            Shake128("abc") != "5881092dd818bf5cf8a3ddb793fbcba74097d5c526a6d35f97b83351940f2cc8")
            throw new CryptographicException("SHAKE128 probe vector failed.");
        if (System.Security.Cryptography.Kmac128.IsSupported)
        {
            Span<byte> nistKey = stackalloc byte[32];
            for (var i = 0; i < nistKey.Length; i++)
                nistKey[i] = (byte)(0x40 + i);
            using var nist = HmacKey.FromBytes(nistKey);
            ReadOnlySpan<byte> nistMsg = [0x00, 0x01, 0x02, 0x03];
            if (KmacBytes(nistMsg, nist, KmacAlgorithm.Kmac128) !=
                "e5780b0d3ea6f7d3a429c5706aa43a00fadbd7d49628839e3187243f456ee14e")
                throw new CryptographicException("KMAC128 probe vector failed.");
        }
        HashingLog.Success("Probe", "Hashing probe complete. Identity=" + Identity);
        return Identity;
    }

    public static string AlgorithmName(HashingAlgorithm algorithm) => algorithm switch
    {
        HashingAlgorithm.Sha256 => "SHA-256",
        HashingAlgorithm.Sha384 => "SHA-384",
        HashingAlgorithm.Sha512 => "SHA-512",
        HashingAlgorithm.Sha3256 => "SHA3-256",
        HashingAlgorithm.Sha3384 => "SHA3-384",
        HashingAlgorithm.Sha3512 => "SHA3-512",
        HashingAlgorithm.Md5 => "MD5",
        HashingAlgorithm.Sha1 => "SHA-1",
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };

    public static int DigestLength(HashingAlgorithm algorithm) => algorithm switch
    {
        HashingAlgorithm.Sha256 or HashingAlgorithm.Sha3256 => 32,
        HashingAlgorithm.Sha384 or HashingAlgorithm.Sha3384 => 48,
        HashingAlgorithm.Sha512 or HashingAlgorithm.Sha3512 => 64,
        HashingAlgorithm.Md5 => 16,
        HashingAlgorithm.Sha1 => 20,
        _ => throw new ArgumentOutOfRangeException(nameof(algorithm)),
    };

    public static bool IsSupported(HashingAlgorithm algorithm) => algorithm switch
    {
        HashingAlgorithm.Sha256 or HashingAlgorithm.Sha384 or HashingAlgorithm.Sha512
            or HashingAlgorithm.Md5 or HashingAlgorithm.Sha1 => true,
        HashingAlgorithm.Sha3256 => SHA3_256.IsSupported,
        HashingAlgorithm.Sha3384 => SHA3_384.IsSupported,
        HashingAlgorithm.Sha3512 => SHA3_512.IsSupported,
        _ => false,
    };

    public static bool IsInterop(HashingAlgorithm algorithm)
        => algorithm is HashingAlgorithm.Md5 or HashingAlgorithm.Sha1;

    public static string HashString(string text, HashingAlgorithm algorithm = HashingAlgorithm.Sha256, HashingTextFormat format = HashingTextFormat.HexLower)
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

    public static string HashBytes(ReadOnlySpan<byte> data, HashingAlgorithm algorithm = HashingAlgorithm.Sha256, HashingTextFormat format = HashingTextFormat.HexLower)
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

    public static bool TryHash(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten, HashingAlgorithm algorithm = HashingAlgorithm.Sha256)
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
}
