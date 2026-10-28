namespace Vestigium.Helpers.SystemInfo;

/// <summary>Base inventory. Fields the host currently extracts for its Summary. No display strings.</summary>
public readonly record struct BaseInventory(
    Fact<ComputerSystemInfo> ComputerSystem,
    Fact<OperatingSystemInfo> OperatingSystem,
    Fact<BiosInfo> Bios,
    Fact<ProcessorInfo> Processor);

/// <summary>Win32_ComputerSystem fields used by the Summary.</summary>
public readonly record struct ComputerSystemInfo(
    string Hostname,
    string Domain,
    int DomainRole,
    string Manufacturer,
    string Model,
    int Sockets,
    int LogicalProcessors,
    ulong TotalPhysicalMemoryBytes,
    string UserName);

/// <summary>Win32_OperatingSystem fields used by the Summary.</summary>
public readonly record struct OperatingSystemInfo(
    int ProductType,
    string Caption,
    string Architecture,
    string Version,
    string BuildNumber,
    DateTime? InstallDate,
    DateTime? LastBootUpTime);

/// <summary>Win32_BIOS fields used by the Summary.</summary>
public readonly record struct BiosInfo(string SerialNumber);

/// <summary>Win32_Processor fields used by the Summary.</summary>
public readonly record struct ProcessorInfo(string Name);
