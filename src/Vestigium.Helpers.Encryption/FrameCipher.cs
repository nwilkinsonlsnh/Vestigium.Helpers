using System.Buffers.Binary;
using System.Security.Cryptography;

namespace Vestigium.Helpers.Encryption;

internal static class FrameCipher
{
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
