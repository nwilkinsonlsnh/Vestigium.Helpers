using System.Diagnostics;

namespace Vestigium.Helpers.Processes;

internal static class ProcessImageReader
{
    internal static ProcessImageType ReadType(nint handle, string? imagePath)
    {
        try
        {
            if (Environment.Is64BitOperatingSystem
                && NativeMethods.IsWow64Process(handle, out var wow64)
                && wow64)
                return ProcessImageType.X86;
        }
        catch
        {
        }

        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            return Environment.Is64BitProcess ? ProcessImageType.X64 : ProcessImageType.X86;

        try
        {
            using var stream = File.OpenRead(imagePath);
            if (stream.Length < 64)
                return ProcessImageType.Unknown;
            var header = new byte[64];
            if (stream.Read(header, 0, 64) < 64)
                return ProcessImageType.Unknown;
            if (header[0] != (byte)'M' || header[1] != (byte)'Z')
                return ProcessImageType.Unknown;
            var pe = BitConverter.ToInt32(header, 60);
            if (pe < 0 || pe + 6 > stream.Length)
                return ProcessImageType.Unknown;
            stream.Position = pe;
            var sig = new byte[6];
            if (stream.Read(sig, 0, 6) < 6)
                return ProcessImageType.Unknown;
            if (sig[0] != (byte)'P' || sig[1] != (byte)'E')
                return ProcessImageType.Unknown;
            var machine = BitConverter.ToUInt16(sig, 4);
            return machine switch
            {
                0x14C => ProcessImageType.X86,
                0x8664 => ProcessImageType.X64,
                0xAA64 => ProcessImageType.Arm64,
                _ => ProcessImageType.Unknown
            };
        }
        catch
        {
            return ProcessImageType.Unknown;
        }
    }

    internal static void ReadVersion(string? imagePath, out string? description, out string? company, out string? version)
    {
        description = company = version = null;
        if (string.IsNullOrWhiteSpace(imagePath) || !File.Exists(imagePath))
            return;
        try
        {
            var info = FileVersionInfo.GetVersionInfo(imagePath);
            description = string.IsNullOrWhiteSpace(info.FileDescription) ? null : info.FileDescription.Trim();
            company = string.IsNullOrWhiteSpace(info.CompanyName) ? null : info.CompanyName.Trim();
            version = string.IsNullOrWhiteSpace(info.FileVersion) ? null : info.FileVersion.Trim();
        }
        catch
        {
        }
    }
}
