namespace Vestigium.Helpers.SystemInfo;

/// <summary>Software inventory. Raw values. Hidden is bool. Host formats.</summary>
public readonly record struct SoftwareInventory(
    Fact<IReadOnlyList<InstalledSoftware>> Installed,
    Fact<IReadOnlyList<Hotfix>> Hotfixes,
    Fact<IReadOnlyList<OptionalFeature>> Features,
    Fact<IReadOnlyList<OdbcEntry>> Odbc,
    Fact<IReadOnlyList<EnvironmentEntry>> Environment,
    Fact<IReadOnlyList<SqlService>> Sql);

/// <summary>Uninstall registry entry. Hidden is system component, child, or no display name.</summary>
public readonly record struct InstalledSoftware(
    string Location,
    string Arch,
    string Name,
    string Version,
    string Publisher,
    string InstallDate,
    bool Hidden);

/// <summary>Win32_QuickFixEngineering.</summary>
public readonly record struct Hotfix(
    string HotFixId,
    string Description,
    string InstalledOn,
    string InstalledBy);

/// <summary>Win32_OptionalFeature. InstallState is the raw CIM value.</summary>
public readonly record struct OptionalFeature(
    string Name,
    uint InstallState);

/// <summary>ODBC driver or DSN. Kind is Driver or DSN.</summary>
public readonly record struct OdbcEntry(
    string Location,
    string Arch,
    string Kind,
    string Name,
    string Detail);

/// <summary>Environment variable. Scope is Process, User, or Machine.</summary>
public readonly record struct EnvironmentEntry(
    string Scope,
    string Name,
    string Value);

/// <summary>SQL-related Windows service. Status and StartType are raw enum values.</summary>
public readonly record struct SqlService(
    string Name,
    string DisplayName,
    uint Status,
    uint StartType);
