using System.Security.Cryptography;

namespace Vestigium.Helpers.Encryption;

/// <summary>
/// RSA-OAEP-SHA256 wrap key. Public always present. Private present only on receive pairs.
/// Minimum 2048-bit; prefer 3072. Never encrypts payload frames.
/// </summary>
public sealed class EncryptionRsaKey : IDisposable
{
    public const int MinBits = 2048;
    public const int PreferredBits = 3072;
    public const int MaxBits = 4096;
    public const byte WrapAlgOaepSha256 = 1;

    private RSA? _rsa;
    private readonly byte[] _spki;
    private readonly byte[]? _pkcs8;
    private bool _disposed;

    private EncryptionRsaKey(RSA rsa, byte[] spki, byte[]? pkcs8, int keyBits)
    {
        _rsa = rsa;
        _spki = spki;
        _pkcs8 = pkcs8;
        KeyBits = keyBits;
        Thumbprint = SHA256.HashData(spki);
    }

    public int KeyBits { get; }
    public byte[] Thumbprint { get; }
    public string ThumbprintHex => Convert.ToHexString(Thumbprint).ToLowerInvariant();
    public bool CanUnwrap => _pkcs8 is not null;

    public static EncryptionRsaKey Generate(int keyBits = PreferredBits)
    {
        if (keyBits is < MinBits or > MaxBits || keyBits % 8 != 0)
            throw new ArgumentOutOfRangeException(nameof(keyBits), "RSA wrap keys are 2048–4096 bits.");
        var rsa = RSA.Create(keyBits);
        try
        {
            return FromRsa(rsa, includePrivate: true);
        }
        catch
        {
            rsa.Dispose();
            throw;
        }
    }

    public static EncryptionRsaKey FromPublicSpki(ReadOnlySpan<byte> spki)
    {
        var rsa = RSA.Create();
        try
        {
            rsa.ImportSubjectPublicKeyInfo(spki, out _);
            return FromRsa(rsa, includePrivate: false);
        }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException)
        {
            rsa.Dispose();
            throw new CryptographicException("The envelope is corrupt.");
        }
    }

    public static EncryptionRsaKey FromPkcs8(ReadOnlySpan<byte> pkcs8)
    {
        var rsa = RSA.Create();
        try
        {
            rsa.ImportPkcs8PrivateKey(pkcs8, out _);
            return FromRsa(rsa, includePrivate: true);
        }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException)
        {
            rsa.Dispose();
            throw new CryptographicException("The envelope is corrupt.");
        }
    }

    public byte[] ExportPublicSpki() => _spki.ToArray();

    public byte[] ExportPkcs8()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_pkcs8 is null)
            throw new InvalidOperationException("Public-only wrap key.");
        return _pkcs8.ToArray();
    }

    public EncryptionRsaKey PublicOnly()
        => FromPublicSpki(_spki);

    public byte[] Wrap(ReadOnlySpan<byte> contentKey32)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (contentKey32.Length != 32)
            throw new ArgumentException("Content key must be 32 bytes.", nameof(contentKey32));
        return _rsa!.Encrypt(contentKey32.ToArray(), RSAEncryptionPadding.OaepSHA256);
    }

    public byte[] Unwrap(ReadOnlySpan<byte> wrapped)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_pkcs8 is null)
            throw new CryptographicException("The envelope is corrupt.");
        try
        {
            var plain = _rsa!.Decrypt(wrapped.ToArray(), RSAEncryptionPadding.OaepSHA256);
            if (plain.Length != 32)
                throw new CryptographicException("The envelope is corrupt.");
            return plain;
        }
        catch (CryptographicException)
        {
            throw new CryptographicException("The envelope is corrupt.");
        }
    }

    public bool ThumbprintEquals(ReadOnlySpan<byte> other)
        => other.Length == 32 && CryptographicOperations.FixedTimeEquals(Thumbprint, other);

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _rsa?.Dispose();
        _rsa = null;
        if (_pkcs8 is not null)
            CryptographicOperations.ZeroMemory(_pkcs8);
    }

    public override string ToString() => $"RSA-{KeyBits} {ThumbprintHex[..8]}…";

    private static EncryptionRsaKey FromRsa(RSA rsa, bool includePrivate)
    {
        var bits = rsa.KeySize;
        if (bits < MinBits || bits > MaxBits)
            throw new CryptographicException("The envelope is corrupt.");
        var spki = rsa.ExportSubjectPublicKeyInfo();
        byte[]? pkcs8 = includePrivate ? rsa.ExportPkcs8PrivateKey() : null;
        return new EncryptionRsaKey(rsa, spki, pkcs8, bits);
    }
}
