namespace Vestigium.Helpers.Encryption;

/// <summary>
/// Optional overwrite of the unencrypted source after a successful Seal.
/// Keep leaves the plaintext file. ThreePass and SevenPass write that many
/// random passes, then one zero pass, then delete the file.
/// </summary>
public enum SecureDeleteMode
{
    Keep = 0,
    ThreePass = 3,
    SevenPass = 7
}
