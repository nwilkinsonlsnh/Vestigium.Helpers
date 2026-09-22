namespace Vestigium.Helpers.Processes;

public enum WindowStatus
{
    None = 0,
    Visible = 1,
    Minimized = 2,
    Maximized = 3,
    Hidden = 4,
    Hung = 5
}

public enum IntegrityLevel
{
    Untrusted = 0,
    Low = 1,
    Medium = 2,
    MediumPlus = 3,
    High = 4,
    System = 5,
    Protected = 6
}

public enum DepStatus
{
    Unknown = 0,
    Disabled = 1,
    Enabled = 2,
    Permanent = 3
}

public enum MitigationState
{
    Unknown = 0,
    Disabled = 1,
    Enabled = 2,
    ExportSuppressed = 3
}

public enum DpiAwareness
{
    Unaware = 0,
    System = 1,
    PerMonitor = 2,
    PerMonitorV2 = 3,
    UnawareGdiScaled = 4
}

public enum SignerTrust
{
    Unknown = 0,
    Verified = 1,
    NotSigned = 2,
    Untrusted = 3,
    Expired = 4,
    Denied = 5
}

public readonly record struct SignerInfo(
    SignerTrust Trust,
    string? Publisher,
    string? Issuer,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo);

public readonly record struct ProcessProtection(string Level, string? Signer);
