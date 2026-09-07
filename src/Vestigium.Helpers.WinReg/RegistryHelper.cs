using System.Runtime.Versioning;
namespace Vestigium.Helpers.WinReg;

/// <summary>
/// Windows Registry read/write helpers. Windows-only. Skeleton surface — behaviour is specified in this project's SRS.
/// </summary>
[SupportedOSPlatform("windows")]
public static class RegistryHelper
{
    /// <summary>
    /// Returns the assembly identity so hosts and tests can prove the library loaded.
    /// </summary>
    public static string Identity => "Vestigium.Helpers.WinReg";
}
