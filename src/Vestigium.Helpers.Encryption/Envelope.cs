using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Vestigium.Helpers.Encryption;

internal sealed class EnvelopeHeader
{
    public byte SuiteMajor { get; init; }
    public byte SuiteMinor { get; init; }
    public byte HeaderMajor { get; init; }
    public byte HeaderMinor { get; init; }
    public byte Alg { get; init; }
    public byte Kdf { get; init; }
    public byte KdfMemMiB { get; init; }
    public byte KdfIter { get; init; }
    public byte KdfPar { get; init; }
    public byte[] Salt { get; init; } = new byte[16];
    public byte[] FileNonce { get; init; } = new byte[12];
    public uint FrameSize { get; init; }
    public ulong FrameCount { get; init; }
    public byte[] PrefixThroughFrameSize { get; init; } = [];
}

internal static class Envelope
{
    public static readonly byte[] HeaderMagic = Encoding.ASCII.GetBytes("VESTIGIUM HDR");
    public static readonly byte[] TrailerMagic = Encoding.ASCII.GetBytes("VESTIGIUM TRL");

    public const byte SuiteMajor = 1;
    public const byte SuiteMinor = 0;
    public const byte SuiteMinorCbc = 1;
    public const byte SuiteMinorRsa = 2;
    public const byte HeaderMajor = 1;
    public const byte HeaderMinor = 0;
    public const byte TrailerMajor = 1;
    public const byte TrailerMinor = 0;
    public const uint FrameSize = 65536;
    public const int TagSize = 16;
    public const int TrailerBodyLength = 465;
    public const int TrailerFieldsLength = 433;
    public const int TrailerFooterLength = 17;
    public const int TrailerTotalLength = TrailerBodyLength + TrailerFooterLength;

    public static byte SuiteMinorFor(byte alg, bool hasRsaWrap = false)
        => hasRsaWrap
            ? SuiteMinorRsa
            : alg == (byte)EncryptionAlgorithm.Aes256CbcHmac ? SuiteMinorCbc : SuiteMinor;

    public static bool LooksLikeHeader(ReadOnlySpan<byte> magic)
        => magic.Length >= 13 && magic[..13].SequenceEqual(HeaderMagic);

    public static bool LooksLikeTrailer(ReadOnlySpan<byte> magic)
        => magic.Length >= 13 && magic[..13].SequenceEqual(TrailerMagic);

    public static byte[] WriteHeader(
        Stream destination,
        byte alg,
        byte kdf,
        byte kdfMem,
        byte kdfIter,
        byte kdfPar,
        ReadOnlySpan<byte> salt,
        ReadOnlySpan<byte> fileNonce,
        ulong frameCount,
        bool hasRsaWrap = false)
    {
        using var buffer = new MemoryStream();
        buffer.Write(HeaderMagic);
        buffer.WriteByte(SuiteMajor);
        buffer.WriteByte(SuiteMinorFor(alg, hasRsaWrap));
        buffer.WriteByte(HeaderMajor);
        buffer.WriteByte(HeaderMinor);
        buffer.WriteByte(alg);
        buffer.WriteByte(kdf);
        buffer.WriteByte(kdfMem);
        buffer.WriteByte(kdfIter);
        buffer.WriteByte(kdfPar);
        if (kdf == 1)
            buffer.Write(salt);
        buffer.Write(fileNonce);
        Span<byte> u32 = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(u32, FrameSize);
        buffer.Write(u32);
        var prefix = buffer.ToArray();
        Span<byte> u64 = stackalloc byte[8];
        BinaryPrimitives.WriteUInt64LittleEndian(u64, frameCount);
        buffer.Write(u64);
        destination.Write(buffer.GetBuffer().AsSpan(0, (int)buffer.Length));
        return prefix;
    }

    public static EnvelopeHeader ReadHeader(Stream source)
    {
        Span<byte> magic = stackalloc byte[13];
        ReadExact(source, magic);
        if (!LooksLikeHeader(magic))
            throw new CryptographicException("The envelope is corrupt.");

        using var prefix = new MemoryStream();
        prefix.Write(magic);
        Span<byte> fixedPart = stackalloc byte[9];
        ReadExact(source, fixedPart);
        prefix.Write(fixedPart);
        var suiteMajor = fixedPart[0];
        var suiteMinor = fixedPart[1];
        var headerMajor = fixedPart[2];
        var headerMinor = fixedPart[3];
        var alg = fixedPart[4];
        var kdf = fixedPart[5];
        var kdfMem = fixedPart[6];
        var kdfIter = fixedPart[7];
        var kdfPar = fixedPart[8];

        if (suiteMajor != SuiteMajor)
            throw new NotSupportedException("suiteMajor");
        if (headerMajor != HeaderMajor)
            throw new NotSupportedException("headerMajor");
        if (alg is not (1 or 2 or 3))
            throw new NotSupportedException("alg");
        if (kdf is not (0 or 1))
            throw new NotSupportedException("kdf");

        var salt = new byte[16];
        if (kdf == 1)
        {
            ReadExact(source, salt);
            prefix.Write(salt);
        }

        var fileNonce = new byte[12];
        ReadExact(source, fileNonce);
        prefix.Write(fileNonce);

        Span<byte> frameSizeBytes = stackalloc byte[4];
        ReadExact(source, frameSizeBytes);
        prefix.Write(frameSizeBytes);
        var frameSize = BinaryPrimitives.ReadUInt32LittleEndian(frameSizeBytes);
        if (frameSize != FrameSize)
            throw new NotSupportedException("frameSize");

        Span<byte> countBytes = stackalloc byte[8];
        ReadExact(source, countBytes);
        var frameCount = BinaryPrimitives.ReadUInt64LittleEndian(countBytes);

        return new EnvelopeHeader
        {
            SuiteMajor = suiteMajor,
            SuiteMinor = suiteMinor,
            HeaderMajor = headerMajor,
            HeaderMinor = headerMinor,
            Alg = alg,
            Kdf = kdf,
            KdfMemMiB = kdfMem,
            KdfIter = kdfIter,
            KdfPar = kdfPar,
            Salt = salt,
            FileNonce = fileNonce,
            FrameSize = frameSize,
            FrameCount = frameCount,
            PrefixThroughFrameSize = prefix.ToArray()
        };
    }

    public static void ReadExact(Stream source, Span<byte> buffer)
    {
        var read = source.ReadAtLeast(buffer, buffer.Length, throwOnEndOfStream: false);
        if (read < buffer.Length)
            throw new CryptographicException("The envelope is corrupt.");
    }
}
