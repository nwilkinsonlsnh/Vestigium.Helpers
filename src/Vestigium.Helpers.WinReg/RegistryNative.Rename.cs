using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Vestigium.Helpers.WinReg;

internal static partial class RegistryNative
{
    [DllImport("advapi32.dll", CharSet = CharSet.Unicode)]
    internal static extern int RegRenameKey(SafeRegistryHandle key, string oldName, string newName);
}
