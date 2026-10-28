namespace Vestigium.Helpers.SystemInfo;

/// <summary>Security inventory. Raw CIM values. No display strings. Host maps labels.</summary>
public readonly record struct SecurityInventory(
    Fact<IReadOnlyList<FirewallProfile>> Profiles,
    Fact<IReadOnlyList<FirewallRule>> Rules,
    Fact<IReadOnlyList<CertificateInfo>> Certificates,
    Fact<IReadOnlyList<DriverInfo>> Drivers);

/// <summary>MSFT_NetFirewallProfile. Enabled and actions are raw CIM values.</summary>
public readonly record struct FirewallProfile(
    string Name,
    uint Enabled,
    uint DefaultInboundAction,
    uint DefaultOutboundAction,
    bool NotifyOnListen,
    bool LogAllowed,
    bool LogBlocked);

/// <summary>MSFT_NetFirewallRule. Enabled, Direction, Action, Profiles are raw CIM values.</summary>
public readonly record struct FirewallRule(
    string DisplayName,
    uint Enabled,
    uint Direction,
    uint Action,
    uint Profiles,
    string Protocol);

/// <summary>Certificate from X509Store. NotAfter is UTC. Location is LM or CU.</summary>
public readonly record struct CertificateInfo(
    string Location,
    string Store,
    string Subject,
    string Issuer,
    string Thumbprint,
    DateTime NotAfter,
    bool HasPrivateKey);

/// <summary>Driver. Source is PnP or System. IsSigned is the raw value from Win32_PnPSignedDriver.</summary>
public readonly record struct DriverInfo(
    string Source,
    string DeviceName,
    string DriverVersion,
    string Manufacturer,
    string IsSigned,
    string Signer,
    string InfName);
