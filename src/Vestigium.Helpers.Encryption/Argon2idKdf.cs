using System.Security.Cryptography;
using System.Text;
using Konscious.Security.Cryptography;

namespace Vestigium.Helpers.Encryption;

internal static class Argon2idKdf
{
    public const byte MemoryMiB = 64;
    public const byte Iterations = 3;
    public const byte Parallelism = 1;
    public const int SaltLength = 16;
    public const int KeyLength = 32;

    public static byte[] Derive(string passphrase, byte[] salt, byte memMiB, byte iter, byte par)
    {
        if (salt.Length != SaltLength || memMiB == 0 || iter == 0 || par == 0)
            throw new CryptographicException("The envelope is corrupt.");

        var password = Encoding.UTF8.GetBytes(passphrase);
        try
        {
            using var argon = new Argon2id(password)
            {
                Salt = salt,
                DegreeOfParallelism = par,
                Iterations = iter,
                MemorySize = memMiB * 1024
            };
            return argon.GetBytes(KeyLength);
        }
        finally
        {
            CryptographicOperations.ZeroMemory(password);
        }
    }
}
