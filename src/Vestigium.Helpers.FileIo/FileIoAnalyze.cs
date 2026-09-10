using Vestigium.Helpers;

namespace Vestigium.Helpers.FileIo;

public sealed class FileIoAnalyzeOptions
{
    public int? MaxDepth { get; set; }
    public IReadOnlyList<string> ExcludeFileMasks { get; set; } = [];
    public IReadOnlyList<string> ExcludeDirectoryMasks { get; set; } = [];
    public long? MinSizeBytes { get; set; }
    public long? MaxSizeBytes { get; set; }
}

public sealed class FileIoBucketCensus
{
    public required FileIoBucket Id { get; init; }
    public required string Name { get; init; }
    public int FileCount { get; init; }
    public long TotalBytes { get; init; }
    public required FileIoSeriesSnapshot Sizes { get; init; }
}

public sealed class FileIoDirectoryAnalysis
{
    public required string Path { get; init; }
    public int FileCount { get; init; }
    public int DirectoryCount { get; init; }
    public long TotalBytes { get; init; }
    public required FileIoSize TotalSize { get; init; }
    public required FileIoSeriesSnapshot FileSizes { get; init; }
    public required IReadOnlyList<FileIoBucketCensus> Buckets { get; init; }
}

public sealed class FileIoProbeOptions
{
    public bool RandomBytes { get; set; } = true;
    public bool KeepProbe { get; set; }
    public string? FileName { get; set; }
}

public sealed class FileIoProbeResult
{
    public required string Path { get; init; }
    public required FileIoSize Size { get; init; }
    public required TimeSpan Duration { get; init; }
    public required double BytesPerSecond { get; init; }
    public required bool Deleted { get; init; }
}

internal static class FileIoBuckets
{
    public static FileIoBucket For(long bytes)
    {
        if (bytes <= 256L * 1024) return FileIoBucket.Tiny;
        if (bytes <= 4L * 1024 * 1024) return FileIoBucket.Small;
        if (bytes <= 32L * 1024 * 1024) return FileIoBucket.Medium;
        if (bytes <= 256L * 1024 * 1024) return FileIoBucket.Large;
        return FileIoBucket.Huge;
    }

    public static string Name(FileIoBucket bucket) => bucket.ToString();
}

internal static class FileIoAnalyzeEngine
{
    public static FileIoDirectoryAnalysis Analyze(string path, FileIoAnalyzeOptions? options)
    {
        var root = HelperGuard.NotBlank(path, nameof(path));
        if (!Directory.Exists(root))
        {
            HelperLog.Reject(HelperLog.AppIds.FileIo, "Analyze", nameof(Analyze), "missing");
            throw new DirectoryNotFoundException("Directory was not found: " + root);
        }

        var o = options ?? new FileIoAnalyzeOptions();
        var sizes = new List<double>();
        var bucketSizes = new List<double>[5];
        var bucketBytes = new long[5];
        for (var i = 0; i < 5; i++)
            bucketSizes[i] = [];
        var files = 0;
        var dirs = 0;
        long total = 0;
        Walk(root, 0, o, ref files, ref dirs, ref total, sizes, bucketSizes, bucketBytes);

        var buckets = new FileIoBucketCensus[5];
        for (var i = 0; i < 5; i++)
        {
            var id = (FileIoBucket)i;
            buckets[i] = new FileIoBucketCensus
            {
                Id = id,
                Name = FileIoBuckets.Name(id),
                FileCount = bucketSizes[i].Count,
                TotalBytes = bucketBytes[i],
                Sizes = FileIoSeriesSnapshot.From(bucketSizes[i], $"file-size-bytes.{id}", "bytes")
            };
        }

        FileIoLog.Success("Analyze", $"path files={files} dirs={dirs} bytes={total}");
        return new FileIoDirectoryAnalysis
        {
            Path = Path.GetFullPath(root),
            FileCount = files,
            DirectoryCount = dirs,
            TotalBytes = total,
            TotalSize = FileIoSize.FromBytes(total),
            FileSizes = FileIoSeriesSnapshot.From(sizes, "file-size-bytes", "bytes"),
            Buckets = buckets
        };
    }

    static void Walk(
        string dir,
        int depth,
        FileIoAnalyzeOptions options,
        ref int files,
        ref int dirs,
        ref long total,
        List<double> sizes,
        List<double>[] bucketSizes,
        long[] bucketBytes)
    {
        dirs++;
        IEnumerable<string> entries;
        try
        {
            entries = Directory.EnumerateFileSystemEntries(dir);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            FileIoLog.Failed("Analyze", $"unauthorized path={dir}");
            return;
        }

        foreach (var entry in entries)
        {
            var name = Path.GetFileName(entry);
            if (Directory.Exists(entry))
            {
                if (Masked(name, options.ExcludeDirectoryMasks))
                    continue;
                if (options.MaxDepth is { } max && depth + 1 >= max)
                    continue;
                Walk(entry, depth + 1, options, ref files, ref dirs, ref total, sizes, bucketSizes, bucketBytes);
            }
            else if (File.Exists(entry))
            {
                if (Masked(name, options.ExcludeFileMasks))
                    continue;
                long length;
                try { length = new FileInfo(entry).Length; }
                catch (IOException) { continue; }
                if (options.MinSizeBytes is { } min && length < min) continue;
                if (options.MaxSizeBytes is { } max && length > max) continue;
                files++;
                total += length;
                sizes.Add(length);
                var bucket = (int)FileIoBuckets.For(length);
                bucketSizes[bucket].Add(length);
                bucketBytes[bucket] += length;
            }
        }
    }

    static bool Masked(string name, IReadOnlyList<string> masks)
    {
        if (masks is null || masks.Count == 0)
            return false;
        foreach (var mask in masks)
        {
            if (string.IsNullOrWhiteSpace(mask)) continue;
            if (name.Contains(mask.Trim('*'), StringComparison.OrdinalIgnoreCase))
                return true;
        }
        return false;
    }
}
