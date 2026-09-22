namespace Vestigium.Helpers.Encryption;

public enum EncryptionAlgorithm
{
    Aes256Gcm = 1,
    ChaCha20Poly1305 = 2,
    /// <summary>v1.1 interop. AES-256-CBC then HMAC-SHA256. Never CBC without HMAC.</summary>
    Aes256CbcHmac = 3
}
