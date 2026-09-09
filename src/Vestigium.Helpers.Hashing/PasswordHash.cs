using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Vestigium.Helpers.Hashing;

/// <summary>
/// Argon2id password verifier (PHC string). Not Encryption's content-key KDF.
/// Default cost is OWASP interactive: 19 MiB / t=2 / p=1. Params live in the string.
/// </summary>
internal static class PasswordHash
{
    public const int MemoryKiB = 19 * 1024;
    public const int Iterations = 2;
    public const int Parallelism = 1;
    public const int SaltLength = 16;
    public const int HashLength = 32;
    public const int PasswordMin = 8;
    public const int PasswordMax = 128;
    public const int VerifyMemoryCapKiB = 64 * 1024;
    public const int VerifyTimeCap = 10;
    public const int VerifyParallelCap = 4;

    public static string Hash(string password)
    {
        ValidatePassword(password);
        var pwd = Encoding.UTF8.GetBytes(password);
        var salt = RandomNumberGenerator.GetBytes(SaltLength);
        try
        {
            var digest = Derive(pwd, salt, MemoryKiB, Iterations, Parallelism);
            try
            {
                return EncodePhc(MemoryKiB, Iterations, Parallelism, salt, digest);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(digest);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pwd);
            CryptographicOperations.ZeroMemory(salt);
        }
    }

    public static bool Verify(string password, string stored)
    {
        ArgumentException.ThrowIfNullOrEmpty(stored);
        if (!TryParsePhc(stored, out var m, out var t, out var p, out var salt, out var expected))
            throw new FormatException("Password verifier is not a valid argon2id PHC string.");
        if (m is < 8 or > VerifyMemoryCapKiB || t is < 1 or > VerifyTimeCap || p is < 1 or > VerifyParallelCap)
            throw new CryptographicException("Password verifier parameters are not acceptable.");

        ValidatePassword(password);
        var pwd = Encoding.UTF8.GetBytes(password);
        try
        {
            var actual = Derive(pwd, salt, m, t, p);
            try
            {
                return CryptographicOperations.FixedTimeEquals(actual, expected);
            }
            finally
            {
                CryptographicOperations.ZeroMemory(actual);
            }
        }
        finally
        {
            CryptographicOperations.ZeroMemory(pwd);
            CryptographicOperations.ZeroMemory(salt);
            CryptographicOperations.ZeroMemory(expected);
        }
    }

    private static void ValidatePassword(string password)
    {
        ArgumentException.ThrowIfNullOrEmpty(password);
        if (password.Length < PasswordMin || password.Length > PasswordMax)
            throw new ArgumentException($"Password must be {PasswordMin}–{PasswordMax} characters.", nameof(password));
    }

    private static byte[] Derive(byte[] password, byte[] salt, int memoryKiB, int iter, int par)
    {
        using var argon = new Argon2id(password)
        {
            Salt = salt,
            DegreeOfParallelism = par,
            Iterations = iter,
            MemorySize = memoryKiB,
        };
        return argon.GetBytes(HashLength);
    }

    private static string EncodePhc(int m, int t, int p, byte[] salt, byte[] hash)
        => $"$argon2id$v=19$m={m},t={t},p={p}${PhcB64(salt)}${PhcB64(hash)}";

    internal static bool TryParsePhc(string stored, out int m, out int t, out int p, out byte[] salt, out byte[] hash)
    {
        m = t = p = 0;
        salt = hash = [];
        var parts = stored.Split('$');
        if (parts.Length != 6)
            return false;
        if (!parts[1].Equals("argon2id", StringComparison.Ordinal))
            return false;
        if (!parts[2].Equals("v=19", StringComparison.Ordinal))
            return false;

        m = t = p = -1;
        foreach (var token in parts[3].Split(','))
        {
            var kv = token.Split('=', 2);
            if (kv.Length != 2)
                return false;
            if (!int.TryParse(kv[1], out var n) || n <= 0)
                return false;
            switch (kv[0])
            {
                case "m": m = n; break;
                case "t": t = n; break;
                case "p": p = n; break;
                default: return false;
            }
        }

        if (m <= 0 || t <= 0 || p <= 0)
            return false;

        try
        {
            salt = PhcB64Decode(parts[4]);
            hash = PhcB64Decode(parts[5]);
        }
        catch (FormatException)
        {
            return false;
        }

        return salt.Length >= 8 && hash.Length == HashLength;
    }

    private static string PhcB64(byte[] data)
        => Convert.ToBase64String(data).TrimEnd('=');

    private static byte[] PhcB64Decode(string text)
    {
        var pad = (4 - (text.Length % 4)) % 4;
        if (pad > 0)
            text += new string('=', pad);
        return Convert.FromBase64String(text);
    }
}
