namespace Vestigium.Helpers.WinReg;

public static partial class RegistryHelper
{
    public static string HashValue(RegistryValueInfo value)
        => RegistryIndexWriter.Hash(value);
}
