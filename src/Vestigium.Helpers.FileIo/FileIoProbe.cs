using System.Diagnostics;
using Vestigium.Helpers;

namespace Vestigium.Helpers.FileIo;

internal static class FileIoProbeEngine
{
    public static FileIoProbeResult Write(string directory, FileIoSize size, FileIoProbeOptions? options)
    {
        var dir = HelperGuard.NotBlank(directory, nameof(directory));
        Directory.CreateDirectory(dir);
        if (size.Bytes < 0)
            throw new ArgumentOutOfRangeException(nameof(size));

        var o = options ?? new FileIoProbeOptions();
        var name = string.IsNullOrWhiteSpace(o.FileName)
            ? "vestigium-probe-" + Guid.NewGuid().ToString("N") + ".bin"
            : o.FileName.Trim();
        var path = Path.Combine(dir, name);
        var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(FileIoJob.StreamBufferSize);
        var sw = Stopwatch.StartNew();
        try
        {
            if (o.RandomBytes)
                Random.Shared.NextBytes(buffer);
            else
                Array.Clear(buffer);

            using (var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None, FileIoJob.StreamBufferSize))
            {
                var remaining = size.Bytes;
                while (remaining > 0)
                {
                    var n = (int)Math.Min(buffer.Length, remaining);
                    stream.Write(buffer, 0, n);
                    remaining -= n;
                }
                stream.Flush(true);
            }
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer, clearArray: true);
        }

        sw.Stop();
        var seconds = Math.Max(sw.Elapsed.TotalSeconds, 0.000001);
        var bps = size.Bytes / seconds;
        var deleted = false;
        if (!o.KeepProbe)
        {
            try { File.Delete(path); deleted = true; } catch (IOException) { }
        }

        FileIoLog.Success("Probe", $"write bytes={size.Bytes} ms={sw.ElapsedMilliseconds} deleted={deleted}");
        return new FileIoProbeResult
        {
            Path = path,
            Size = size,
            Duration = sw.Elapsed,
            BytesPerSecond = bps,
            Deleted = deleted
        };
    }

    public static FileIoProbeResult Read(string path)
    {
        var file = HelperGuard.FileExists(path, nameof(path));
        var length = new FileInfo(file).Length;
        var buffer = System.Buffers.ArrayPool<byte>.Shared.Rent(FileIoJob.StreamBufferSize);
        var sw = Stopwatch.StartNew();
        try
        {
            using var stream = new FileStream(file, FileMode.Open, FileAccess.Read, FileShare.Read, FileIoJob.StreamBufferSize);
            var remaining = length;
            while (remaining > 0)
            {
                var n = stream.Read(buffer, 0, (int)Math.Min(buffer.Length, remaining));
                if (n <= 0) break;
                remaining -= n;
            }
        }
        finally
        {
            System.Buffers.ArrayPool<byte>.Shared.Return(buffer);
        }

        sw.Stop();
        var seconds = Math.Max(sw.Elapsed.TotalSeconds, 0.000001);
        FileIoLog.Success("Probe", $"read bytes={length} ms={sw.ElapsedMilliseconds}");
        return new FileIoProbeResult
        {
            Path = file,
            Size = FileIoSize.FromBytes(length),
            Duration = sw.Elapsed,
            BytesPerSecond = length / seconds,
            Deleted = false
        };
    }
}
