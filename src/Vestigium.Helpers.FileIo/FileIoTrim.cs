using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Vestigium.Helpers.FileIo;

internal static class FileIoTrim
{
    private const uint FileLevelTrim = 0x00098208;

    public static bool Try(string path)
    {
        try
        {
            var length = new FileInfo(path).Length;
            if (length <= 0)
                return false;
            var input = new byte[16 + 16];
            BitConverter.TryWriteBytes(input.AsSpan(8), 1);
            BitConverter.TryWriteBytes(input.AsSpan(16), 0L);
            BitConverter.TryWriteBytes(input.AsSpan(24), length);
            using var handle = File.OpenHandle(path, FileMode.Open, FileAccess.Write, FileShare.None);
            return DeviceIoControl(handle, FileLevelTrim, input, input.Length, null, 0, out _, IntPtr.Zero);
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static string Rename(string path)
    {
        var current = path;
        var folder = Path.GetDirectoryName(path) ?? "";
        for (var i = 0; i < 4; i++)
        {
            var next = Path.Combine(folder, Guid.NewGuid().ToString("N"));
            File.Move(current, next);
            current = next;
        }
        return current;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle handle,
        uint code,
        byte[] input,
        int inputSize,
        byte[]? output,
        int outputSize,
        out int returned,
        IntPtr overlapped);
}
