using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Vestigium.Helpers.Encryption;

public enum EncryptionKeyRole
{
    Receive = 1,
    Send = 2
}

public enum EncryptionIssuedToKind
{
    Organization = 1,
    Person = 2,
    Service = 3,
    Host = 4
}

public enum EncryptionKeyStatus
{
    Active = 1,
    Retired = 2,
    Compromised = 3,
    Disabled = 4,
    Expired = 5
}

public sealed class EncryptionKeyRecord
{
    public const int TitleMax = 75;
    public const int SubjectMax = 50;
    public const int DescriptionMax = 220;
    public const int IssuedToMax = 75;
    public const int ApplicationMax = 50;

    public Guid Id { get; init; }
    public string Title { get; init; } = "";
    public string Subject { get; init; } = "";
    public string Description { get; init; } = "";
    public string IssuedTo { get; init; } = "";
    public EncryptionIssuedToKind IssuedToKind { get; init; } = EncryptionIssuedToKind.Organization;
    public string? Application { get; init; }
    public EncryptionKeyRole Role { get; init; }
    public EncryptionKeyStatus Status { get; internal set; } = EncryptionKeyStatus.Active;
    public DateTimeOffset? ExpiresUtc { get; internal set; }
    public DateTimeOffset? StatusChangedUtc { get; internal set; }
    public int KeyBits { get; init; }
    public string ThumbprintSha256 { get; init; } = "";
    public bool Escrow { get; init; }

    /// <summary>Public always. Private only on receive pairs or escrow.</summary>
    [JsonIgnore]
    public EncryptionRsaKey Key { get; init; } = null!;

    internal static string Clamp(string value, int max, string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, name);
        value = value.Trim();
        if (value.Length > max)
            throw new ArgumentException($"{name} is limited to {max} characters.", name);
        return value;
    }
}

/// <summary>
/// Named RSA wrap keys. Pairs (private) receive; contacts (public) send.
/// Same issuedTo may hold many keys (AppX vs AppY). Trailer stores thumbprints only.
/// </summary>
public sealed class EncryptionKeyRing : IDisposable
{
    public const string Format = "VESTIGIUM-KEYRING";
    public const int FormatMajor = 1;
    public const int FormatMinor = 1;

    private readonly List<EncryptionKeyRecord> _pairs = [];
    private readonly List<EncryptionKeyRecord> _contacts = [];
    private bool _disposed;

    public Guid RingId { get; private set; } = Guid.NewGuid();
    public string Title { get; private set; } = "Vestigium key ring";
    public IReadOnlyList<EncryptionKeyRecord> Pairs => _pairs;
    public IReadOnlyList<EncryptionKeyRecord> Contacts => _contacts;

    public static EncryptionKeyRing Create(string title)
    {
        var ring = new EncryptionKeyRing
        {
            Title = EncryptionKeyRecord.Clamp(title, EncryptionKeyRecord.TitleMax, nameof(title))
        };
        return ring;
    }

    public EncryptionKeyRecord AddPair(
        string title,
        string subject,
        string issuedTo,
        string? description = null,
        EncryptionIssuedToKind kind = EncryptionIssuedToKind.Organization,
        string? application = null,
        int keyBits = EncryptionRsaKey.PreferredBits)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var key = EncryptionRsaKey.Generate(keyBits);
        var row = NewRecord(title, subject, description, issuedTo, kind, application, EncryptionKeyRole.Receive, key, escrow: false);
        _pairs.Add(row);
        return row;
    }

    public EncryptionKeyRecord AddContact(
        string title,
        string subject,
        string issuedTo,
        EncryptionRsaKey publicKey,
        string? description = null,
        EncryptionIssuedToKind kind = EncryptionIssuedToKind.Organization,
        string? application = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentNullException.ThrowIfNull(publicKey);
        var pub = publicKey.PublicOnly();
        var row = NewRecord(title, subject, description, issuedTo, kind, application, EncryptionKeyRole.Send, pub, escrow: false);
        _contacts.Add(row);
        return row;
    }

    /// <summary>
    /// Generate a pair for a company/app, store the public as a contact, return the private for a one-time slip.
    /// Private is not kept unless <paramref name="escrow"/> is true.
    /// </summary>
    public (EncryptionKeyRecord Contact, EncryptionRsaKey PrivateExport) Issue(
        string title,
        string subject,
        string issuedTo,
        string? description = null,
        EncryptionIssuedToKind kind = EncryptionIssuedToKind.Organization,
        string? application = null,
        int keyBits = EncryptionRsaKey.PreferredBits,
        bool escrow = false)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var generated = EncryptionRsaKey.Generate(keyBits);
        var contact = AddContact(title, subject, issuedTo, generated, description, kind, application);
        if (escrow)
        {
            var pair = NewRecord(title, subject, description, issuedTo, kind, application, EncryptionKeyRole.Receive, generated, escrow: true);
            _pairs.Add(pair);
        }

        var export = EncryptionRsaKey.FromPkcs8(generated.ExportPkcs8());
        if (!escrow)
            generated.Dispose();
        EncryptionLog.Success("Issue", $"{EncryptionAudit.TokenLabel(contact)} bits={keyBits} escrow={escrow}");
        return (contact, export);
    }

    public EncryptionKeyStatus EffectiveStatus(EncryptionKeyRecord row)
    {
        ArgumentNullException.ThrowIfNull(row);
        if (row.Status is EncryptionKeyStatus.Disabled or EncryptionKeyStatus.Expired
            or EncryptionKeyStatus.Retired or EncryptionKeyStatus.Compromised)
            return row.Status;
        if (row.ExpiresUtc is { } expiry && expiry <= DateTimeOffset.UtcNow)
            return EncryptionKeyStatus.Expired;
        return EncryptionKeyStatus.Active;
    }

    public void Enable(EncryptionKeyRecord row, DateTimeOffset? expiresUtc = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var match = Locate(row);
        RefuseTerminal(match, EncryptionTokenUse.Seal);
        ApplyStatus(match, EncryptionKeyStatus.Active, expiresUtc);
        EncryptionLog.Success("Enable", $"{EncryptionAudit.TokenLabel(match)} status=Active");
    }

    public void Disable(EncryptionKeyRecord row)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var match = Locate(row);
        RefuseTerminal(match, EncryptionTokenUse.Seal);
        ApplyStatus(match, EncryptionKeyStatus.Disabled, match.ExpiresUtc);
        EncryptionLog.TokenWarning("Disable", $"{EncryptionAudit.TokenLabel(match)} status=Disabled");
    }

    public void Expire(EncryptionKeyRecord row, DateTimeOffset? at = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var match = Locate(row);
        RefuseTerminal(match, EncryptionTokenUse.Seal);
        var when = at ?? DateTimeOffset.UtcNow;
        if (when <= DateTimeOffset.UtcNow)
        {
            ApplyStatus(match, EncryptionKeyStatus.Expired, when);
            EncryptionLog.TokenWarning("Expire", $"{EncryptionAudit.TokenLabel(match)} status=Expired");
        }
        else
        {
            ApplyStatus(match, EncryptionKeyStatus.Active, when);
            EncryptionLog.TokenWarning("Expire", $"{EncryptionAudit.TokenLabel(match)} status=Active until={when:O}");
        }
    }

    public void Retire(EncryptionKeyRecord row)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var match = Locate(row);
        if (match.Status == EncryptionKeyStatus.Compromised)
            throw new EncryptionTokenException(match.Id, EncryptionKeyStatus.Compromised, EncryptionTokenUse.Seal);
        ApplyStatus(match, EncryptionKeyStatus.Retired, match.ExpiresUtc);
        EncryptionLog.TokenWarning("Retire", $"{EncryptionAudit.TokenLabel(match)} status=Retired");
    }

    public void Compromise(EncryptionKeyRecord row)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var match = Locate(row);
        ApplyStatus(match, EncryptionKeyStatus.Compromised, match.ExpiresUtc);
        EncryptionLog.TokenWarning("Compromise", $"{EncryptionAudit.TokenLabel(match)} status=Compromised");
    }

    public EncryptionRsaKey RequireForSeal(EncryptionKeyRecord row, EncryptionKeyOverride? keyOverride = null)
        => Require(row, EncryptionTokenUse.Seal, keyOverride);

    public EncryptionRsaKey RequireForOpen(EncryptionKeyRecord row, EncryptionKeyOverride? keyOverride = null)
        => Require(row, EncryptionTokenUse.Open, keyOverride);

    public EncryptionRsaKey Require(EncryptionKeyRecord row, EncryptionTokenUse use, EncryptionKeyOverride? keyOverride = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var match = Locate(row);
        var effective = EffectiveStatus(match);
        if (effective == EncryptionKeyStatus.Expired && match.Status != EncryptionKeyStatus.Expired)
            ApplyStatus(match, EncryptionKeyStatus.Expired, match.ExpiresUtc);

        if (use == EncryptionTokenUse.Open && !match.Key.CanUnwrap)
            throw new CryptographicException("The envelope is corrupt.");

        if (effective == EncryptionKeyStatus.Active)
            return match.Key;

        if (effective is EncryptionKeyStatus.Disabled or EncryptionKeyStatus.Expired)
        {
            if (keyOverride is null)
            {
                EncryptionLog.TokenWarning("Refuse", $"{EncryptionAudit.TokenLabel(match)} status={effective} use={use}");
                throw new EncryptionTokenException(match.Id, effective, use);
            }

            EncryptionLog.TokenWarning(
                "Override",
                $"{EncryptionAudit.TokenLabel(match)} status={effective} use={use} by={keyOverride.RequestedBy} reason={keyOverride.Reason}");
            return match.Key;
        }

        EncryptionLog.TokenWarning("Refuse", $"{EncryptionAudit.TokenLabel(match)} status={effective} use={use}");
        throw new EncryptionTokenException(match.Id, effective, use);
    }

    public EncryptionRsaKey? FindPrivate(ReadOnlySpan<byte> thumbprint)
        => FindPrivate(thumbprint, keyOverride: null);

    public EncryptionRsaKey? FindPrivate(ReadOnlySpan<byte> thumbprint, EncryptionKeyOverride? keyOverride)
    {
        foreach (var row in _pairs)
        {
            if (!row.Key.CanUnwrap || !row.Key.ThumbprintEquals(thumbprint))
                continue;
            return Require(row, EncryptionTokenUse.Open, keyOverride);
        }

        return null;
    }

    public EncryptionKeyRecord? FindByThumbprintHex(string hex)
    {
        hex = hex.Trim().ToLowerInvariant();
        foreach (var row in _pairs.Concat(_contacts))
        {
            if (row.ThumbprintSha256 == hex)
                return row;
        }

        return null;
    }

    public string ExportPublicSlip(EncryptionKeyRecord record)
    {
        ArgumentNullException.ThrowIfNull(record);
        var slip = new SlipDto
        {
            Format = Format,
            Id = record.Id,
            Title = record.Title,
            Subject = record.Subject,
            Description = record.Description,
            IssuedTo = record.IssuedTo,
            IssuedToKind = record.IssuedToKind.ToString(),
            Application = record.Application,
            KeyBits = record.KeyBits,
            Wrap = "RSA-OAEP-SHA256",
            ThumbprintSha256 = record.ThumbprintSha256,
            PublicSpki = Convert.ToBase64String(record.Key.ExportPublicSpki())
        };
        return JsonSerializer.Serialize(slip, JsonOptions);
    }

    public string ToJson()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var dto = new RingDto
        {
            Format = Format,
            FormatMajor = FormatMajor,
            FormatMinor = FormatMinor,
            RingId = RingId,
            Title = Title,
            Pairs = _pairs.Select(ToDto).ToList(),
            Contacts = _contacts.Select(ToDto).ToList()
        };
        return JsonSerializer.Serialize(dto, JsonOptions);
    }

    public static EncryptionKeyRing FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(json);
        var dto = JsonSerializer.Deserialize<RingDto>(json, JsonOptions)
                  ?? throw new CryptographicException("The envelope is corrupt.");
        if (dto.Format != Format || dto.FormatMajor != FormatMajor)
            throw new NotSupportedException("keyring");
        var ring = new EncryptionKeyRing { RingId = dto.RingId == Guid.Empty ? Guid.NewGuid() : dto.RingId };
        if (!string.IsNullOrWhiteSpace(dto.Title))
            ring.Title = EncryptionKeyRecord.Clamp(dto.Title, EncryptionKeyRecord.TitleMax, nameof(Title));
        foreach (var row in dto.Pairs ?? [])
            ring._pairs.Add(FromDto(row, EncryptionKeyRole.Receive));
        foreach (var row in dto.Contacts ?? [])
            ring._contacts.Add(FromDto(row, EncryptionKeyRole.Send));
        return ring;
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        foreach (var row in _pairs.Concat(_contacts))
            row.Key.Dispose();
    }

    private EncryptionKeyRecord Locate(EncryptionKeyRecord row)
    {
        ArgumentNullException.ThrowIfNull(row);
        foreach (var existing in _pairs.Concat(_contacts))
        {
            if (existing.Id == row.Id || existing.ThumbprintSha256 == row.ThumbprintSha256)
                return existing;
        }

        throw new ArgumentException("Token is not on this ring.", nameof(row));
    }

    private static void RefuseTerminal(EncryptionKeyRecord match, EncryptionTokenUse use)
    {
        if (match.Status is EncryptionKeyStatus.Compromised or EncryptionKeyStatus.Retired)
            throw new EncryptionTokenException(match.Id, match.Status, use);
    }

    private void ApplyStatus(EncryptionKeyRecord match, EncryptionKeyStatus status, DateTimeOffset? expiresUtc)
    {
        var now = DateTimeOffset.UtcNow;
        foreach (var row in _pairs.Concat(_contacts))
        {
            if (row.ThumbprintSha256 != match.ThumbprintSha256)
                continue;
            row.Status = status;
            row.ExpiresUtc = expiresUtc;
            row.StatusChangedUtc = now;
        }
    }

    private static EncryptionKeyRecord NewRecord(
        string title,
        string subject,
        string? description,
        string issuedTo,
        EncryptionIssuedToKind kind,
        string? application,
        EncryptionKeyRole role,
        EncryptionRsaKey key,
        bool escrow)
        => new()
        {
            Id = Guid.NewGuid(),
            Title = EncryptionKeyRecord.Clamp(title, EncryptionKeyRecord.TitleMax, nameof(title)),
            Subject = EncryptionKeyRecord.Clamp(subject, EncryptionKeyRecord.SubjectMax, nameof(subject)),
            Description = string.IsNullOrWhiteSpace(description)
                ? ""
                : EncryptionKeyRecord.Clamp(description, EncryptionKeyRecord.DescriptionMax, nameof(description)),
            IssuedTo = EncryptionKeyRecord.Clamp(issuedTo, EncryptionKeyRecord.IssuedToMax, nameof(issuedTo)),
            IssuedToKind = kind,
            Application = string.IsNullOrWhiteSpace(application)
                ? null
                : EncryptionKeyRecord.Clamp(application, EncryptionKeyRecord.ApplicationMax, nameof(application)),
            Role = role,
            Status = EncryptionKeyStatus.Active,
            ExpiresUtc = null,
            StatusChangedUtc = DateTimeOffset.UtcNow,
            KeyBits = key.KeyBits,
            ThumbprintSha256 = key.ThumbprintHex,
            Escrow = escrow,
            Key = key
        };

    private static RecordDto ToDto(EncryptionKeyRecord row) => new()
    {
        Id = row.Id,
        Title = row.Title,
        Subject = row.Subject,
        Description = row.Description,
        IssuedTo = row.IssuedTo,
        IssuedToKind = row.IssuedToKind.ToString(),
        Application = row.Application,
        Role = row.Role.ToString(),
        Status = row.Status.ToString(),
        ExpiresUtc = row.ExpiresUtc,
        StatusChangedUtc = row.StatusChangedUtc,
        KeyBits = row.KeyBits,
        ThumbprintSha256 = row.ThumbprintSha256,
        Escrow = row.Escrow,
        PublicSpki = Convert.ToBase64String(row.Key.ExportPublicSpki()),
        PrivatePkcs8 = row.Key.CanUnwrap ? Convert.ToBase64String(row.Key.ExportPkcs8()) : null
    };

    private static EncryptionKeyRecord FromDto(RecordDto row, EncryptionKeyRole fallbackRole)
    {
        EncryptionRsaKey key;
        if (!string.IsNullOrWhiteSpace(row.PrivatePkcs8))
            key = EncryptionRsaKey.FromPkcs8(Convert.FromBase64String(row.PrivatePkcs8));
        else
            key = EncryptionRsaKey.FromPublicSpki(Convert.FromBase64String(row.PublicSpki ?? throw new CryptographicException("The envelope is corrupt.")));
        return new EncryptionKeyRecord
        {
            Id = row.Id == Guid.Empty ? Guid.NewGuid() : row.Id,
            Title = EncryptionKeyRecord.Clamp(row.Title ?? "key", EncryptionKeyRecord.TitleMax, "title"),
            Subject = EncryptionKeyRecord.Clamp(row.Subject ?? "subject", EncryptionKeyRecord.SubjectMax, "subject"),
            Description = string.IsNullOrWhiteSpace(row.Description) ? "" : EncryptionKeyRecord.Clamp(row.Description, EncryptionKeyRecord.DescriptionMax, "description"),
            IssuedTo = EncryptionKeyRecord.Clamp(row.IssuedTo ?? "unknown", EncryptionKeyRecord.IssuedToMax, "issuedTo"),
            IssuedToKind = Enum.TryParse<EncryptionIssuedToKind>(row.IssuedToKind, out var kind) ? kind : EncryptionIssuedToKind.Organization,
            Application = string.IsNullOrWhiteSpace(row.Application) ? null : EncryptionKeyRecord.Clamp(row.Application, EncryptionKeyRecord.ApplicationMax, "application"),
            Role = Enum.TryParse<EncryptionKeyRole>(row.Role, out var role) ? role : fallbackRole,
            Status = Enum.TryParse<EncryptionKeyStatus>(row.Status, out var status) ? status : EncryptionKeyStatus.Active,
            ExpiresUtc = row.ExpiresUtc,
            StatusChangedUtc = row.StatusChangedUtc,
            KeyBits = key.KeyBits,
            ThumbprintSha256 = key.ThumbprintHex,
            Escrow = row.Escrow,
            Key = key
        };
    }

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    private sealed class RingDto
    {
        public string Format { get; set; } = "";
        public int FormatMajor { get; set; }
        public int FormatMinor { get; set; }
        public Guid RingId { get; set; }
        public string Title { get; set; } = "";
        public List<RecordDto>? Pairs { get; set; }
        public List<RecordDto>? Contacts { get; set; }
    }

    private sealed class RecordDto
    {
        public Guid Id { get; set; }
        public string? Title { get; set; }
        public string? Subject { get; set; }
        public string? Description { get; set; }
        public string? IssuedTo { get; set; }
        public string? IssuedToKind { get; set; }
        public string? Application { get; set; }
        public string? Role { get; set; }
        public string? Status { get; set; }
        public DateTimeOffset? ExpiresUtc { get; set; }
        public DateTimeOffset? StatusChangedUtc { get; set; }
        public int KeyBits { get; set; }
        public string? ThumbprintSha256 { get; set; }
        public bool Escrow { get; set; }
        public string? PublicSpki { get; set; }
        public string? PrivatePkcs8 { get; set; }
    }

    private sealed class SlipDto
    {
        public string Format { get; set; } = "";
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string Subject { get; set; } = "";
        public string Description { get; set; } = "";
        public string IssuedTo { get; set; } = "";
        public string IssuedToKind { get; set; } = "";
        public string? Application { get; set; }
        public int KeyBits { get; set; }
        public string Wrap { get; set; } = "";
        public string ThumbprintSha256 { get; set; } = "";
        public string PublicSpki { get; set; } = "";
    }
}

internal sealed class RsaWrapRecord
{
    public byte WrapAlg { get; init; }
    public ushort KeyBits { get; init; }
    public byte[] Thumbprint { get; init; } = new byte[32];
    public byte[] WrappedKey { get; init; } = [];
}
