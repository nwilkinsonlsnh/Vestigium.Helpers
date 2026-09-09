namespace Vestigium.Helpers.Hashing;

/// <summary>
/// Generated / required HMAC-SHA256 key length in bytes.
/// HMAC is not AES: these are lengths, not algorithm variants.
/// 64 is the SHA-256 block; 128 is hashed down internally.
/// </summary>
public enum HmacKeySize
{
    Bytes16 = 16,
    Bytes32 = 32,
    Bytes64 = 64,
    Bytes128 = 128,
}
