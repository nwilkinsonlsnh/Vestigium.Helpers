using System.Security.Principal;

namespace Vestigium.Helpers.Watch.Dns;

public static class Program
{
    public const int NotElevated = 2;

    public static int Main()
    {
        if (!Elevation.IsElevated())
            return NotElevated;

        return 0;
    }
}

public static class Elevation
{
    public static bool IsElevated()
    {
        if (!OperatingSystem.IsWindows())
            return false;

        using var identity = WindowsIdentity.GetCurrent();
        return new WindowsPrincipal(identity).IsInRole(WindowsBuiltInRole.Administrator);
    }
}
