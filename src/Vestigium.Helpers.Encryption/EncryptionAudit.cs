namespace Vestigium.Helpers.Encryption;

internal static class EncryptionAudit
{
    public const int ActorMax = 50;
    public const int ReasonMax = 80;

    public static string TokenLabel(EncryptionKeyRecord row)
        => $"key={Prefix(row.ThumbprintSha256)}";

    public static string Prefix(string thumbHex)
    {
        if (string.IsNullOrWhiteSpace(thumbHex))
            return "????????";
        thumbHex = thumbHex.Trim().ToLowerInvariant();
        return thumbHex.Length <= 8 ? thumbHex : thumbHex[..8];
    }

    public static string Actor(string requestedBy)
    {
        var value = EncryptionKeyRecord.Clamp(requestedBy, ActorMax, nameof(requestedBy));
        if (LooksLikeSecret(value))
            throw new ArgumentException("RequestedBy must not contain key material.", nameof(requestedBy));
        return value;
    }

    public static string Reason(string reason)
    {
        var value = EncryptionKeyRecord.Clamp(reason, ReasonMax, nameof(reason));
        if (LooksLikeSecret(value))
            throw new ArgumentException("Override reason must not contain key material.", nameof(reason));
        return value;
    }

    public static bool LooksLikeSecret(string value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return false;
        if (value.Contains("BEGIN ", StringComparison.OrdinalIgnoreCase)
            || value.Contains("PRIVATE KEY", StringComparison.OrdinalIgnoreCase)
            || value.Contains("PKCS8", StringComparison.OrdinalIgnoreCase)
            || value.Contains("-----"))
            return true;

        var compact = value.Replace(" ", "", StringComparison.Ordinal).Replace("\n", "", StringComparison.Ordinal);
        if (compact.Length >= 32 && compact.All(Uri.IsHexDigit))
            return true;
        if (compact.Length >= 44 && compact.All(IsBase64Char))
            return true;
        return false;
    }

    private static bool IsBase64Char(char c)
        => char.IsLetterOrDigit(c) || c is '+' or '/' or '=' or '-' or '_';
}
