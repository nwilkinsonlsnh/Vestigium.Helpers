namespace Vestigium.Helpers.WinReg;

public enum RegistryHiveKind
{
    ClassesRoot = 0,
    CurrentUser = 1,
    LocalMachine = 2,
    Users = 3,
    CurrentConfig = 4
}

public enum RegistryViewKind
{
    Default = 0,
    Registry64 = 1,
    Registry32 = 2
}

public enum RegistryDetailLevel
{
    Identity = 0,
    Slim = 1,
    Full = 2
}

public enum RegistryValueKind
{
    None = 0,
    String = 1,
    ExpandString = 2,
    Binary = 3,
    DWord = 4,
    MultiString = 7,
    QWord = 11,
    Unknown = -1
}

public enum RegistryWriteStatus
{
    Ok = 0,
    Denied = 1,
    NotFound = 2,
    InvalidPath = 3,
    TypeMismatch = 4,
    InUse = 5,
    Unsupported = 6
}

public enum RegistryExportFormat
{
    RegFile = 0,
    HiveFile = 1
}

public readonly record struct RegistryFieldAvailability(string Field, string State, string? Reason);

public sealed class RegistryKeyInfo
{
    public string? Machine { get; init; }
    public required RegistryHiveKind Hive { get; init; }
    public required string Path { get; init; }
    public required string Name { get; init; }
    public RegistryViewKind View { get; init; }
    public int? SubKeyCount { get; init; }
    public int? ValueCount { get; init; }
    public DateTimeOffset? LastWriteTime { get; init; }
    public IReadOnlyList<string> SubKeyNames { get; init; } = [];
    public IReadOnlyList<RegistryValueInfo> Values { get; init; } = [];
    public IReadOnlyList<RegistryFieldAvailability> Availability { get; init; } = [];
}

public sealed class RegistryValueInfo
{
    public required string Name { get; init; }
    public bool IsDefault { get; init; }
    public RegistryValueKind Type { get; init; }
    public object? Data { get; init; }
    public string? DataText { get; init; }
}

public readonly record struct RegistryWriteResult(
    RegistryWriteStatus Status,
    RegistryHiveKind Hive,
    string Path,
    string? ValueName,
    string? Reason);
