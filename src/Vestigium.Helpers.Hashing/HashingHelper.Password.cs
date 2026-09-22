using System.Security.Cryptography;

namespace Vestigium.Helpers.Hashing;

public static partial class HashingHelper
{
    public static string HashPassword(string password)
    {
        using var scope = HashingLog.Begin("HashPassword", "argon2id");
        HashingLog.Pending("HashPassword", "argon2id m=19456 t=2 p=1");
        try
        {
            var stored = PasswordHash.Hash(password);
            HashingLog.Success("HashPassword", "password hashed");
            return stored;
        }
        catch (Exception ex) when (ex is ArgumentException)
        {
            HashingLog.Failed("Password hash rejected.");
            throw;
        }
    }

    public static bool VerifyPassword(string password, string stored)
    {
        using var scope = HashingLog.Begin("VerifyPassword", "argon2id");
        HashingLog.Pending("VerifyPassword", "argon2id");
        if (string.IsNullOrEmpty(stored)
            || !PasswordHash.TryParsePhc(stored, out _, out _, out _, out _, out _))
        {
            HashingLog.Failed("Password verifier rejected.");
            return false;
        }

        try
        {
            var ok = PasswordHash.Verify(password, stored);
            HashingLog.Success("VerifyPassword", ok ? "password verify ok" : "password verify failed");
            return ok;
        }
        catch (Exception ex) when (ex is CryptographicException or ArgumentException)
        {
            HashingLog.Failed("Password verify rejected.");
            throw;
        }
    }
}
