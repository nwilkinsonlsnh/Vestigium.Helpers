namespace Vestigium.Helpers.Hashing;

/// <summary>
/// NIST SP 800-185 KMAC. Not HMAC. Default is KMAC128 with a 32-byte tag.
/// KMAC256 default tag is 64 bytes. Same <see cref="HmacKey"/> (min 16). OS-gated.
/// </summary>
public enum KmacAlgorithm
{
    Kmac128 = 1,
    Kmac256 = 2,
}
