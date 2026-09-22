using System.Security.Cryptography;
using System.Text;

namespace Vestigium.Helpers.Encryption;

public sealed class EncryptionSecret : IDisposable
{
    private string? _passphrase;
    private byte[]? _key;
    private bool _disposed;

    private EncryptionSecret(string passphrase)
    {
        _passphrase = passphrase;
        IsPassphrase = true;
    }

    private EncryptionSecret(byte[] key)
    {
        _key = key;
        IsPassphrase = false;
    }

    public bool IsPassphrase { get; }

    public static EncryptionSecret FromPassphrase(string passphrase)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passphrase);
        return new EncryptionSecret(passphrase);
    }

    public static EncryptionSecret FromKey(ReadOnlySpan<byte> key32)
    {
        if (key32.Length != 32)
            throw new ArgumentException("Raw keys must be 32 bytes.", nameof(key32));
        return new EncryptionSecret(key32.ToArray());
    }

    internal byte[] DeriveContentKey(byte kdf, byte[] salt, byte memMiB, byte iter, byte par)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (kdf == 0)
        {
            if (_key is null)
                throw new CryptographicException("The envelope is corrupt.");
            var copy = new byte[32];
            Buffer.BlockCopy(_key, 0, copy, 0, 32);
            return copy;
        }

        if (kdf != 1)
            throw new NotSupportedException("kdf");

        if (_passphrase is null)
            throw new CryptographicException("The envelope is corrupt.");

        return Argon2idKdf.Derive(_passphrase, salt, memMiB, iter, par);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        if (_key is not null)
        {
            CryptographicOperations.ZeroMemory(_key);
            _key = null;
        }

        _passphrase = null;
    }

    public override string ToString() => nameof(EncryptionSecret);
}
