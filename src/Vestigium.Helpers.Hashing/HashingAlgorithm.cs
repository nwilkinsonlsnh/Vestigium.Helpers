namespace Vestigium.Helpers.Hashing;

/// <summary>
/// Unkeyed digest. HMAC is a different method family (<see cref="HashingHelper.HmacString"/>).
/// SHA-256 is the default. MD5 and SHA-1 are interop only.
/// </summary>
public enum HashingAlgorithm
{
    Sha256 = 1,
    Sha384 = 2,
    Sha512 = 3,
    Sha3_256 = 4,
    Sha3_384 = 5,
    Sha3_512 = 6,
    Md5 = 10,
    Sha1 = 11,
}
