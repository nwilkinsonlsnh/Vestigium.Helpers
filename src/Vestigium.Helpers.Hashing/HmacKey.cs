using System.Security.Cryptography;
using System.Text;

namespace Vestigium.Helpers.Hashing;

/// <summary>
/// HMAC-SHA256 key. Generate random bytes, take UTF-8 of a typed secret, or decode Base64.
/// Minimum 16 bytes. Default generate size is 32. Dispose zeros the key.
/// Passing a Base64 string to <see cref="FromString"/> treats the letters as the key — use <see cref="FromBase64"/>.
/// </summary>
public sealed class HmacKey : IDisposable
{
    public const int MinimumLength = 16;
    public const HmacKeySize DefaultSize = HmacKeySize.Bytes32;

    private byte[]? _bytes;
    private bool _disposed;

    private HmacKey(byte[] bytes) => _bytes = bytes;

    public int Length
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _bytes!.Length;
        }
    }

    internal ReadOnlySpan<byte> Span
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _bytes!;
        }
    }

    public static HmacKey Generate(HmacKeySize size = DefaultSize)
    {
        EnsureSize(size);
        return new HmacKey(RandomNumberGenerator.GetBytes((int)size));
    }

    public static HmacKey FromString(string utf8, HmacKeySize? required = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(utf8);
        var bytes = Encoding.UTF8.GetBytes(utf8);
        try
        {
            ValidateLength(bytes.Length, required, nameof(utf8));
            var copy = new byte[bytes.Length];
            Buffer.BlockCopy(bytes, 0, copy, 0, bytes.Length);
            return new HmacKey(copy);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(bytes);
        }
    }

    public static HmacKey FromBase64(string base64, HmacKeySize? required = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(base64);
        byte[] bytes;
        try
        {
            bytes = Convert.FromBase64String(base64.Trim());
        }
        catch (FormatException ex)
        {
            throw new ArgumentException("HMAC key Base64 is not valid.", nameof(base64), ex);
        }

        ValidateLength(bytes.Length, required, nameof(base64));
        return new HmacKey(bytes);
    }

    public static HmacKey FromBytes(ReadOnlySpan<byte> bytes, HmacKeySize? required = null)
    {
        ValidateLength(bytes.Length, required, nameof(bytes));
        return new HmacKey(bytes.ToArray());
    }

    public string ToHexLower()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Convert.ToHexString(_bytes!).ToLowerInvariant();
    }

    public string ToBase64()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return Convert.ToBase64String(_bytes!);
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        if (_bytes is not null)
            CryptographicOperations.ZeroMemory(_bytes);
        _bytes = null;
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    private static void EnsureSize(HmacKeySize size)
    {
        if (size is not (HmacKeySize.Bytes16 or HmacKeySize.Bytes32 or HmacKeySize.Bytes64 or HmacKeySize.Bytes128))
            throw new ArgumentOutOfRangeException(nameof(size), "HMAC key size must be 16, 32, 64, or 128 bytes.");
    }

    private static void ValidateLength(int length, HmacKeySize? required, string param)
    {
        if (length < MinimumLength)
            throw new ArgumentException($"HMAC key must be at least {MinimumLength} bytes.", param);
        if (required is { } size)
        {
            EnsureSize(size);
            if (length != (int)size)
                throw new ArgumentException($"HMAC key must be exactly {(int)size} bytes.", param);
        }
    }
}
