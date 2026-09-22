namespace Vestigium.Helpers.Hashing;

/// <summary>
/// SHAKE XOF (FIPS 202). Unkeyed. Not HMAC and not <see cref="HashingHelper.HashString"/>.
/// SHAKE128 default output is 32 bytes; SHAKE256 default is 64. OS-gated.
/// </summary>
public enum ShakeAlgorithm
{
    Shake128 = 1,
    Shake256 = 2,
}
