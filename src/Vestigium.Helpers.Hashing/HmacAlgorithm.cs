namespace Vestigium.Helpers.Hashing;

/// <summary>
/// Keyed MAC. Default is HMAC-SHA256 (32 bytes). SHA-384 is 48, SHA-512 is 64.
/// Not HMAC-SHA3. Encryption structural <c>mac</c> and CBC frame HMAC stay Encryption.
/// </summary>
public enum HmacAlgorithm
{
    Sha256 = 1,
    Sha384 = 2,
    Sha512 = 3,
}
