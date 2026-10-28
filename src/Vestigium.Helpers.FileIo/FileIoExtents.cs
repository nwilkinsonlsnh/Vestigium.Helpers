using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Win32.SafeHandles;

namespace Vestigium.Helpers.FileIo;

internal static class FileIoExtents
{
    private const uint RetrievalPointers = 0x00090073;

    public static bool Awkward(string path)
    {
        var attributes = File.GetAttributes(path);
        return attributes.HasFlag(FileAttributes.Compressed)
            || attributes.HasFlag(FileAttributes.SparseFile)
            || attributes.HasFlag(FileAttributes.Encrypted);
    }

    public static bool Overwrite(string path, FileIoShredRecipe recipe, byte[] buffer, Action<double>? progress = null)
    {
        var extents = Read(path);
        if (extents.Count == 0)
            return false;

        var root = Path.GetPathRoot(path);
        if (string.IsNullOrWhiteSpace(root) || !Cluster(root, out var cluster))
            return false;

        var volume = @"\\.\" + root.TrimEnd('\\');
        try
        {
            using var disk = new FileStream(volume, FileMode.Open, FileAccess.ReadWrite, FileShare.ReadWrite);
            var passes = Math.Max(recipe.Passes.Length, 1);
            var total = extents.Sum(extent => extent.Clusters) * cluster;
            for (var pass = 0; pass < recipe.Passes.Length; pass++)
            {
                Fill(buffer, recipe.Passes[pass]);
                var written = 0L;
                foreach (var extent in extents)
                {
                    disk.Position = extent.Lcn * cluster;
                    var remaining = extent.Clusters * cluster;
                    while (remaining > 0)
                    {
                        var n = (int)Math.Min(buffer.Length, remaining);
                        disk.Write(buffer, 0, n);
                        remaining -= n;
                        written += n;
                        if (total > 0)
                            progress?.Invoke((pass + written / (double)total) / passes);
                    }
                }
                disk.Flush(true);
            }
            return true;
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

    private static void Fill(byte[] buffer, string pass)
    {
        if (pass.Equals("Zero", StringComparison.OrdinalIgnoreCase))
            Array.Clear(buffer);
        else
            System.Security.Cryptography.RandomNumberGenerator.Fill(buffer);
    }

    private static List<Extent> Read(string path)
    {
        var found = new List<Extent>();
        using var file = File.OpenHandle(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        var input = new Vcn();
        var output = new byte[1024 * 64];
        while (DeviceIoControl(file, RetrievalPointers, ref input, Marshal.SizeOf<Vcn>(), output, output.Length, out var returned, IntPtr.Zero))
        {
            if (returned < 16)
                break;
            var count = BitConverter.ToInt32(output, 0);
            var vcn = BitConverter.ToInt64(output, 8);
            for (var i = 0; i < count && 16 + (i + 1) * 16 <= returned; i++)
            {
                var next = BitConverter.ToInt64(output, 16 + i * 16);
                var lcn = BitConverter.ToInt64(output, 24 + i * 16);
                if (lcn >= 0 && next > vcn)
                    found.Add(new Extent(lcn, next - vcn));
                vcn = next;
            }
            break;
        }
        return found;
    }

    private static bool Cluster(string root, out long bytes)
    {
        bytes = 0;
        if (!GetDiskFreeSpace(root, out var sectors, out var sectorBytes, out _, out _))
            return false;
        bytes = (long)sectors * sectorBytes;
        return bytes > 0;
    }

    private readonly record struct Extent(long Lcn, long Clusters);

    [StructLayout(LayoutKind.Sequential)]
    private struct Vcn
    {
        public long StartingVcn;
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern bool DeviceIoControl(
        SafeFileHandle handle,
        uint code,
        ref Vcn input,
        int inputSize,
        byte[] output,
        int outputSize,
        out int returned,
        IntPtr overlapped);

    [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
    private static extern bool GetDiskFreeSpace(
        string root,
        out uint sectorsPerCluster,
        out uint bytesPerSector,
        out uint freeClusters,
        out uint totalClusters);
}
