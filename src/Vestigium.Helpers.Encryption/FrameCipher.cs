using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Vestigium.Helpers.Encryption;

internal static class FrameCipher
{
    public const int CbcIvSize = 16;
    public const int CbcHmacSize = 32;
    public const int CbcBlockSize = 16;

    private static ReadOnlySpan<byte> CbcMacInfo => "VESTIGIUM-CBC-HMAC"u8;

    public static bool IsAead(EncryptionAlgorithm alg)
        => alg is EncryptionAlgorithm.Aes256Gcm or EncryptionAlgorithm.ChaCha20Poly1305;

    public static bool IsSupported(EncryptionAlgorithm alg)
        => alg is EncryptionAlgorithm.Aes256Gcm
            or EncryptionAlgorithm.ChaCha20Poly1305
            or EncryptionAlgorithm.Aes256CbcHmac;

    public static EncryptionAlgorithm NameAlgorithm(EncryptionAlgorithm alg)
        => alg == EncryptionAlgorithm.Aes256CbcHmac ? EncryptionAlgorithm.Aes256Gcm : alg;

    public static int CbcPaddedLength(int plaintextLength)
    {
        var pad = CbcBlockSize - (plaintextLength % CbcBlockSize);
        return plaintextLength + pad;
    }

    public static void Encrypt(
        EncryptionAlgorithm alg,
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> aad,
        ReadOnlySpan<byte> plaintext,
        Span<byte> ciphertext,
        Span<byte> tag)
    {
        switch (alg)
        {
            case EncryptionAlgorithm.Aes256Gcm:
                using (var gcm = new AesGcm(key, Envelope.TagSize))
                    gcm.Encrypt(nonce, plaintext, ciphertext, tag, aad);
                break;
            case EncryptionAlgorithm.ChaCha20Poly1305:
                using (var cha = new ChaCha20Poly1305(key))
                    cha.Encrypt(nonce, plaintext, ciphertext, tag, aad);
                break;
            default:
                throw new NotSupportedException("alg");
        }
    }

    public static void Decrypt(
        EncryptionAlgorithm alg,
        ReadOnlySpan<byte> key,
        ReadOnlySpan<byte> nonce,
        ReadOnlySpan<byte> aad,
        ReadOnlySpan<byte> ciphertext,
        ReadOnlySpan<byte> tag,
        Span<byte> plaintext)
    {
        try
        {
            switch (alg)
            {
                case EncryptionAlgorithm.Aes256Gcm:
                    using (var gcm = new AesGcm(key, Envelope.TagSize))
                        gcm.Decrypt(nonce, ciphertext, tag, plaintext, aad);
                    break;
                case EncryptionAlgorithm.ChaCha20Poly1305:
                    using (var cha = new ChaCha20Poly1305(key))
                        cha.Decrypt(nonce, ciphertext, tag, plaintext, aad);
                    break;
                default:
                    throw new NotSupportedException("alg");
            }
        }
        catch (CryptographicException)
        {
            throw new CryptographicException("The envelope is corrupt.");
        }
    }

    public static void WriteCbcFrame(
        Stream destination,
        ReadOnlySpan<byte> contentKey,
        ReadOnlySpan<byte> fileNonce,
        ReadOnlySpan<byte> headerPrefix,
        uint index,
        ReadOnlySpan<byte> plaintext,
        IncrementalHash? callerMac = null)
    {
        Span<byte> iv = stackalloc byte[CbcIvSize];
        RandomNumberGenerator.Fill(iv);
        var key = contentKey.ToArray();
        var ivBytes = iv.ToArray();
        byte[] cipher;
        try
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = ivBytes;
            using var enc = aes.CreateEncryptor();
            var plain = plaintext.ToArray();
            try
            {
                cipher = enc.TransformFinalBlock(plain, 0, plain.Length);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(plain);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(ivBytes);
        }

        var mac = CbcFrameMac(contentKey, fileNonce, headerPrefix, index, iv, cipher);
        try
        {
            destination.Write(iv);
            destination.Write(cipher);
            destination.Write(mac);
            callerMac?.AppendData(iv);
            callerMac?.AppendData(cipher);
            callerMac?.AppendData(mac);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(cipher);
            CryptographicOperations.ZeroMemory(mac);
        }
    }

    public static void AbsorbCbcFrameBytes(Stream source, int expectedPlain, IncrementalHash callerMac)
    {
        Span<byte> iv = stackalloc byte[CbcIvSize];
        Envelope.ReadExact(source, iv);
        callerMac.AppendData(iv);
        var padded = CbcPaddedLength(expectedPlain);
        var cipher = new byte[padded];
        Envelope.ReadExact(source, cipher);
        callerMac.AppendData(cipher);
        Span<byte> mac = stackalloc byte[CbcHmacSize];
        Envelope.ReadExact(source, mac);
        callerMac.AppendData(mac);
        CryptographicOperations.ZeroMemory(cipher);
    }

    public static int ReadCbcFrame(
        Stream source,
        ReadOnlySpan<byte> contentKey,
        ReadOnlySpan<byte> fileNonce,
        ReadOnlySpan<byte> headerPrefix,
        uint index,
        int expectedPlain,
        Span<byte> plaintext)
    {
        if (expectedPlain < 0 || expectedPlain > plaintext.Length)
            throw new CryptographicException("The envelope is corrupt.");

        Span<byte> iv = stackalloc byte[CbcIvSize];
        Envelope.ReadExact(source, iv);
        var padded = CbcPaddedLength(expectedPlain);
        var cipher = new byte[padded];
        Envelope.ReadExact(source, cipher);
        Span<byte> mac = stackalloc byte[CbcHmacSize];
        Envelope.ReadExact(source, mac);

        var expected = CbcFrameMac(contentKey, fileNonce, headerPrefix, index, iv, cipher);
        try
        {
            if (!CryptographicOperations.FixedTimeEquals(expected, mac))
                throw new CryptographicException("The envelope is corrupt.");
        }
        finally
        {
            CryptographicOperations.ZeroMemory(expected);
        }

        var key = contentKey.ToArray();
        var ivBytes = iv.ToArray();
        byte[] plain;
        try
        {
            using var aes = Aes.Create();
            aes.KeySize = 256;
            aes.Mode = CipherMode.CBC;
            aes.Padding = PaddingMode.PKCS7;
            aes.Key = key;
            aes.IV = ivBytes;
            using var dec = aes.CreateDecryptor();
            try
            {
                plain = dec.TransformFinalBlock(cipher, 0, cipher.Length);
            }
            catch (CryptographicException)
            {
                throw new CryptographicException("The envelope is corrupt.");
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(key);
            CryptographicOperations.ZeroMemory(ivBytes);
            CryptographicOperations.ZeroMemory(cipher);
        }

        try
        {
            if (plain.Length != expectedPlain)
                throw new CryptographicException("The envelope is corrupt.");
            plain.CopyTo(plaintext);
            return plain.Length;
        }
        finally
        {
            CryptographicOperations.ZeroMemory(plain);
        }
    }

    public static byte[] CbcFrameMac(
        ReadOnlySpan<byte> contentKey,
        ReadOnlySpan<byte> fileNonce,
        ReadOnlySpan<byte> headerPrefix,
        uint index,
        ReadOnlySpan<byte> iv,
        ReadOnlySpan<byte> ciphertext)
    {
        Span<byte> macKey = stackalloc byte[32];
        HKDF.DeriveKey(HashAlgorithmName.SHA256, contentKey, macKey, fileNonce, CbcMacInfo);
        var indexBytes = new byte[4];
        BinaryPrimitives.WriteUInt32BigEndian(indexBytes, index);
        var input = new byte[headerPrefix.Length + 4 + iv.Length + ciphertext.Length];
        headerPrefix.CopyTo(input);
        indexBytes.CopyTo(input.AsSpan(headerPrefix.Length));
        iv.CopyTo(input.AsSpan(headerPrefix.Length + 4));
        ciphertext.CopyTo(input.AsSpan(headerPrefix.Length + 4 + iv.Length));
        try
        {
            return HMACSHA256.HashData(macKey, input);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(macKey);
            CryptographicOperations.ZeroMemory(input);
        }
    }

    public static byte[] FrameNonce(ReadOnlySpan<byte> fileNonce, uint index)
    {
        var nonce = fileNonce.ToArray();
        BinaryPrimitives.WriteUInt32BigEndian(nonce.AsSpan(8, 4), index);
        return nonce;
    }

    public static byte[] FrameAad(ReadOnlySpan<byte> headerPrefix, uint index)
    {
        var aad = new byte[headerPrefix.Length + 4];
        headerPrefix.CopyTo(aad);
        BinaryPrimitives.WriteUInt32BigEndian(aad.AsSpan(headerPrefix.Length), index);
        return aad;
    }

    public static byte[] NameAad()
    {
        var prefix = "VESTIGIUM-ORIG-NAME"u8;
        var aad = new byte[prefix.Length + 2];
        prefix.CopyTo(aad);
        aad[prefix.Length] = Envelope.SuiteMajor;
        aad[prefix.Length + 1] = Envelope.SuiteMinor;
        return aad;
    }
}
