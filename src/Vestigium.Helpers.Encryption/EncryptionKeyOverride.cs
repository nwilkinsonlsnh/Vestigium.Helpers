namespace Vestigium.Helpers.Encryption;

public enum EncryptionTokenUse
{
    Seal = 1,
    Open = 2
}

/// <summary>
/// Operator request to use a disabled or expired token. Compromised and retired tokens cannot be overridden.
/// Reason is an audit justification, not a secret — PEM / long hex / long Base64 are rejected.
/// </summary>
public sealed class EncryptionKeyOverride
{
    private EncryptionKeyOverride(string requestedBy, string reason, DateTimeOffset requestedUtc)
    {
        RequestedBy = requestedBy;
        Reason = reason;
        RequestedUtc = requestedUtc;
    }

    public string RequestedBy { get; }
    public string Reason { get; }
    public DateTimeOffset RequestedUtc { get; }

    public static EncryptionKeyOverride Request(string requestedBy, string reason)
        => new(
            EncryptionAudit.Actor(requestedBy),
            EncryptionAudit.Reason(reason),
            DateTimeOffset.UtcNow);
}

public sealed class EncryptionTokenException : InvalidOperationException
{
    public EncryptionTokenException(Guid tokenId, EncryptionKeyStatus status, EncryptionTokenUse use)
        : base(MessageFor(status))
    {
        TokenId = tokenId;
        Status = status;
        Use = use;
    }

    public Guid TokenId { get; }
    public EncryptionKeyStatus Status { get; }
    public EncryptionTokenUse Use { get; }

    private static string MessageFor(EncryptionKeyStatus status) => status switch
    {
        EncryptionKeyStatus.Disabled => "The token is disabled.",
        EncryptionKeyStatus.Expired => "The token is expired.",
        _ => "The token is not usable."
    };
}
