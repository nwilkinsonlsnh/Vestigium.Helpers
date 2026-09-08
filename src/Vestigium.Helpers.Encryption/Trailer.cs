using System.Buffers.Binary;
using System.Security.Cryptography;
using System.Text;

namespace Vestigium.Helpers.Encryption;

internal sealed class TrailerFields
{
    public byte SuiteMajor { get; init; }
    public byte SuiteMinor { get; init; }
    public byte TrailerMajor { get; init; }
    public byte TrailerMinor { get; init; }
    public byte Alg { get; init; }
    public byte Kdf { get; init; }
    public byte KdfMemMiB { get; init; }
    public byte KdfIter { get; init; }
    public byte KdfPar { get; init; }
    public ushort Flags { get; init; }
    public byte[] Salt { get; init; } = new byte[16];
    public byte[] FileNonce { get; init; } = new byte[12];
    public uint FrameSize { get; init; }
    public ulong FrameCount { get; init; }
    public ulong PlaintextLength { get; init; }
    public long CreatedUtc { get; init; }
    public byte[] Sha256 { get; init; } = new byte[32];
    public byte[] HmacSha256 { get; init; } = new byte[32];
    public byte[] NameNonce { get; init; } = new byte[12];
    public ushort NameLen { get; init; }
    public byte[] NameCt { get; init; } = new byte[272];
    public byte[] Mac { get; init; } = new byte[32];
    public byte[] Body { get; init; } = [];

    public bool UnknownLength => (Flags & 1) != 0;
    public bool HasHiddenName => (Flags & 2) != 0;
    public bool Sha256Filled => (Flags & 8) != 0 || Sha256.Any(b => b != 0);
    public bool HmacFilled => (Flags & 16) != 0 || HmacSha256.Any(b => b != 0);
}

internal static class Trailer
{
    public const ushort FlagUnknownLength = 1;
    public const ushort FlagHasName = 2;
    public const int NamePlainSize = 256;
    public const int NameCtSize = 272;

    public static byte[] Write(
        Stream destination,
        byte alg,
        byte kdf,
        byte kdfMem,
        byte kdfIter,
        byte kdfPar,
        ushort flags,
        ReadOnlySpan<byte> salt,
        ReadOnlySpan<byte> fileNonce,
        ulong frameCount,
        ulong plaintextLen,
        long createdUtc,
        ReadOnlySpan<byte> nameNonce,
        ushort nameLen,
        ReadOnlySpan<byte> nameCt,
        ReadOnlySpan<byte> contentKey)
    {
        var body = new byte[Envelope.TrailerBodyLength];
        var w = 0;
        body[w++] = Envelope.SuiteMajor;
        body[w++] = Envelope.SuiteMinor;
        body[w++] = Envelope.TrailerMajor;
        body[w++] = Envelope.TrailerMinor;
        body[w++] = alg;
        body[w++] = kdf;
        body[w++] = kdfMem;
        body[w++] = kdfIter;
        body[w++] = kdfPar;
        BinaryPrimitives.WriteUInt16LittleEndian(body.AsSpan(w), flags);
        w += 2;
        salt[..16].CopyTo(body.AsSpan(w));
        w += 16;
        fileNonce[..12].CopyTo(body.AsSpan(w));
        w += 12;
        BinaryPrimitives.WriteUInt32LittleEndian(body.AsSpan(w), Envelope.FrameSize);
        w += 4;
        BinaryPrimitives.WriteUInt64LittleEndian(body.AsSpan(w), frameCount);
        w += 8;
        BinaryPrimitives.WriteUInt64LittleEndian(body.AsSpan(w), plaintextLen);
        w += 8;
        BinaryPrimitives.WriteInt64LittleEndian(body.AsSpan(w), createdUtc);
        w += 8;
        w += 32; // sha256 zeros
        w += 32; // hmac reserved zeros
        w += 16; // reserved
        nameNonce[..12].CopyTo(body.AsSpan(w));
        w += 12;
        BinaryPrimitives.WriteUInt16LittleEndian(body.AsSpan(w), nameLen);
        w += 2;
        nameCt[..NameCtSize].CopyTo(body.AsSpan(w));
        w += NameCtSize;
        var mac = ComputeMac(contentKey, fileNonce, body.AsSpan(0, w));
        mac.CopyTo(body.AsSpan(w));

        destination.Write(body);
        Span<byte> len = stackalloc byte[4];
        BinaryPrimitives.WriteUInt32LittleEndian(len, Envelope.TrailerBodyLength);
        destination.Write(len);
        destination.Write(Envelope.TrailerMagic);
        return body;
    }

    public static bool TryRead(Stream source, out TrailerFields trailer)
    {
        trailer = null!;
        if (!source.CanSeek || source.Length - source.Position < Envelope.TrailerFooterLength)
            return false;

        var end = source.Length;
        source.Position = end - Envelope.TrailerFooterLength;
        Span<byte> footer = stackalloc byte[Envelope.TrailerFooterLength];
        Envelope.ReadExact(source, footer);
        var bodyLength = BinaryPrimitives.ReadUInt32LittleEndian(footer);
        if (!Envelope.LooksLikeTrailer(footer[4..]))
            return false;
        if (bodyLength < 32 || end - Envelope.TrailerFooterLength - bodyLength < 0)
            return false;

        var start = end - Envelope.TrailerFooterLength - bodyLength;
        if (start < 0)
            return false;
        source.Position = start;
        var body = new byte[bodyLength];
        Envelope.ReadExact(source, body);
        if (body.Length < Envelope.TrailerBodyLength)
            return false;

        try
        {
            trailer = ParseBody(body);
            return true;
        }
        catch (Exception ex) when (ex is CryptographicException or NotSupportedException or ArgumentException)
        {
            return false;
        }
    }

    public static TrailerFields Read(Stream source)
    {
        if (!TryRead(source, out var trailer))
            throw new CryptographicException("The envelope is corrupt.");
        return trailer;
    }

    public static TrailerFields ParseBody(byte[] body)
    {
        if (body.Length < Envelope.TrailerBodyLength)
            throw new CryptographicException("The envelope is corrupt.");

        var r = 0;
        var suiteMajor = body[r++];
        var suiteMinor = body[r++];
        var trailerMajor = body[r++];
        var trailerMinor = body[r++];
        if (suiteMajor != Envelope.SuiteMajor)
            throw new NotSupportedException("suiteMajor");
        if (trailerMajor != Envelope.TrailerMajor)
            throw new NotSupportedException("trailerMajor");

        var alg = body[r++];
        var kdf = body[r++];
        var kdfMem = body[r++];
        var kdfIter = body[r++];
        var kdfPar = body[r++];
        if (alg is not (1 or 2))
            throw new NotSupportedException("alg");
        if (kdf is not (0 or 1))
            throw new NotSupportedException("kdf");

        var flags = BinaryPrimitives.ReadUInt16LittleEndian(body.AsSpan(r));
        r += 2;
        var salt = body.AsSpan(r, 16).ToArray();
        r += 16;
        var fileNonce = body.AsSpan(r, 12).ToArray();
        r += 12;
        var frameSize = BinaryPrimitives.ReadUInt32LittleEndian(body.AsSpan(r));
        r += 4;
        if (frameSize != Envelope.FrameSize)
            throw new NotSupportedException("frameSize");
        var frameCount = BinaryPrimitives.ReadUInt64LittleEndian(body.AsSpan(r));
        r += 8;
        var plaintextLen = BinaryPrimitives.ReadUInt64LittleEndian(body.AsSpan(r));
        r += 8;
        var created = BinaryPrimitives.ReadInt64LittleEndian(body.AsSpan(r));
        r += 8;
        var sha = body.AsSpan(r, 32).ToArray();
        r += 32;
        var hmac = body.AsSpan(r, 32).ToArray();
        r += 32;
        r += 16;
        var nameNonce = body.AsSpan(r, 12).ToArray();
        r += 12;
        var nameLen = BinaryPrimitives.ReadUInt16LittleEndian(body.AsSpan(r));
        r += 2;
        var nameCt = body.AsSpan(r, NameCtSize).ToArray();
        r += NameCtSize;
        var mac = body.AsSpan(r, 32).ToArray();

        return new TrailerFields
        {
            SuiteMajor = suiteMajor,
            SuiteMinor = suiteMinor,
            TrailerMajor = trailerMajor,
            TrailerMinor = trailerMinor,
            Alg = alg,
            Kdf = kdf,
            KdfMemMiB = kdfMem,
            KdfIter = kdfIter,
            KdfPar = kdfPar,
            Flags = flags,
            Salt = salt,
            FileNonce = fileNonce,
            FrameSize = frameSize,
            FrameCount = frameCount,
            PlaintextLength = plaintextLen,
            CreatedUtc = created,
            Sha256 = sha,
            HmacSha256 = hmac,
            NameNonce = nameNonce,
            NameLen = nameLen,
            NameCt = nameCt,
            Mac = mac,
            Body = body
        };
    }

    public static byte[] ComputeMac(ReadOnlySpan<byte> contentKey, ReadOnlySpan<byte> fileNonce, ReadOnlySpan<byte> bodyWithoutMac)
    {
        Span<byte> macKey = stackalloc byte[32];
        HKDF.DeriveKey(HashAlgorithmName.SHA256, contentKey, macKey, fileNonce, "VESTIGIUM-TRL-HMAC"u8);
        try
        {
            return HMACSHA256.HashData(macKey, bodyWithoutMac);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(macKey);
        }
    }

    public static bool VerifyMac(ReadOnlySpan<byte> contentKey, TrailerFields trailer)
    {
        var expected = ComputeMac(contentKey, trailer.FileNonce, trailer.Body.AsSpan(0, Envelope.TrailerBodyLength - 32));
        return CryptographicOperations.FixedTimeEquals(expected, trailer.Mac);
    }

    public static void SealOriginalName(
        EncryptionAlgorithm alg,
        ReadOnlySpan<byte> contentKey,
        string originalName,
        out byte[] nameNonce,
        out ushort nameLen,
        out byte[] nameCt)
    {
        var utf8 = Encoding.UTF8.GetBytes(OriginalNames.Validate(originalName));
        nameLen = (ushort)utf8.Length;
        var padded = new byte[NamePlainSize];
        utf8.CopyTo(padded, 0);
        nameNonce = RandomNumberGenerator.GetBytes(12);
        nameCt = new byte[NameCtSize];
        FrameCipher.Encrypt(
            alg,
            contentKey,
            nameNonce,
            FrameCipher.NameAad(),
            padded,
            nameCt.AsSpan(0, NamePlainSize),
            nameCt.AsSpan(NamePlainSize, Envelope.TagSize));
        CryptographicOperations.ZeroMemory(padded);
        CryptographicOperations.ZeroMemory(utf8);
    }

    public static string? OpenOriginalName(EncryptionAlgorithm alg, ReadOnlySpan<byte> contentKey, TrailerFields trailer)
    {
        if (!trailer.HasHiddenName || trailer.NameLen == 0)
            return null;
        if (trailer.NameLen > NamePlainSize)
            throw new CryptographicException("The envelope is corrupt.");

        var padded = new byte[NamePlainSize];
        try
        {
            FrameCipher.Decrypt(
                alg,
                contentKey,
                trailer.NameNonce,
                FrameCipher.NameAad(),
                trailer.NameCt.AsSpan(0, NamePlainSize),
                trailer.NameCt.AsSpan(NamePlainSize, Envelope.TagSize),
                padded);
            var name = Encoding.UTF8.GetString(padded, 0, trailer.NameLen);
            return OriginalNames.Validate(name);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(padded);
        }
    }
}
