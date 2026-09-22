namespace Vestigium.Helpers.Hashing;

/// <summary>
/// Keyed HMAC. Default is HMAC-SHA256 (32 bytes). SHA-384 is 48, SHA-512 is 64.
/// HMAC-SHA3-256 / 384 / 512 are opt-in (OS-gated). Not KMAC and not SHAKE.
/// Encryption structural <c>mac</c> and CBC frame HMAC stay Encryption.
/// </summary>
public enum HmacAlgorithm
{
    Sha256 = 1,
    Sha384 = 2,
    Sha512 = 3,
    Sha3_256 = 4,
    Sha3_384 = 5,
    Sha3_512 = 6,
}
