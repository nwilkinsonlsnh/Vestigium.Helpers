using System.Buffers;
using System.Buffers.Binary;
using System.IO.Hashing;
using System.Security.Cryptography;

namespace Vestigium.Helpers.Hashing;

public static partial class HashingHelper
{
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
        HmacAlgorithm.Sha3_256 => HashAlgorithmName.SHA3_256,
        HmacAlgorithm.Sha3_384 => HashAlgorithmName.SHA3_384,
        HmacAlgorithm.Sha3_512 => HashAlgorithmName.SHA3_512,
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
        Stream stream, HashingAlgorithm algorithm, CancellationToken cancellationToken)
    {
        using var hasher = IncrementalHash.CreateHash(AlgName(algorithm));
        var buffer = ArrayPool<byte>.Shared.Rent(StreamBufferSize);
        long total = 0;
        try
        {
            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, StreamBufferSize), cancellationToken).ConfigureAwait(false);
                if (read == 0) break;
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
        var hasher = CreateChecksum(algorithm);
        bytes = PumpChecksum(stream, hasher);
        var digest = hasher.GetHashAndReset();
        return algorithm is ChecksumAlgorithm.Crc32 ? ReverseCopy(digest) : digest;
    }

    private static async Task<(byte[] digest, long bytes)> ChecksumStreamAsync(
        Stream stream, ChecksumAlgorithm algorithm, CancellationToken cancellationToken)
    {
        var hasher = CreateChecksum(algorithm);
        var buffer = ArrayPool<byte>.Shared.Rent(StreamBufferSize);
        long total = 0;
        try
        {
            while (true)
            {
                var read = await stream.ReadAsync(buffer.AsMemory(0, StreamBufferSize), cancellationToken).ConfigureAwait(false);
                if (read == 0) break;
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

    private static void EnsureHmacSupported(HmacAlgorithm algorithm)
    {
        if (!IsHmacSupported(algorithm))
            throw new NotSupportedException($"{HmacName(algorithm)} is not available on this OS.");
    }

    private static string VisiblePath(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "(stream)";
        try { return Path.GetFileName(path); }
        catch (ArgumentException) { return "(path)"; }
    }
}
