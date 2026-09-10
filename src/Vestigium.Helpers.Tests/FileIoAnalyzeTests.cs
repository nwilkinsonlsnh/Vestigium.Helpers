using Vestigium.Helpers.FileIo;

namespace Vestigium.Helpers.Tests;

public sealed class FileIoAnalyzeTests
{
    [Fact]
    public void Size_2048_MiB_is_2_GiB()
    {
        var size = FileIoSize.From(2048, FileIoSizeUnit.MiB);
        Assert.Equal(2L * 1024 * 1024 * 1024, size.Bytes);
        Assert.Equal("2 GiB", size.Display);
        Assert.Equal("2048 MiB", size.InputDisplay);
    }

    [Fact]
    public void Size_2048_MB_is_SI_not_binary()
    {
        var size = FileIoSize.From(2048, FileIoSizeUnit.MB);
        Assert.Equal(2_048_000_000L, size.Bytes);
        Assert.Contains("GB", size.Display, StringComparison.Ordinal);
        Assert.NotEqual(FileIoSize.From(2, FileIoSizeUnit.GiB).Bytes, size.Bytes);
    }

    [Fact]
    public void Size_caller_sets_TB_GB_MB()
    {
        Assert.Equal(1_000_000_000_000L, FileIoSize.From(1, FileIoSizeUnit.TB).Bytes);
        Assert.Equal(500_000_000L, FileIoSize.From(500, FileIoSizeUnit.MB).Bytes);
        Assert.Equal(14L * 1_000_000_000_000L, FileIoSize.From(14, FileIoSizeUnit.TB).Bytes);
        Assert.Equal(1_024, FileIoSize.From(1, FileIoSizeUnit.KiB).Bytes);
        Assert.Equal(1_000, FileIoSize.From(1, FileIoSizeUnit.KB).Bytes);
        Assert.Equal(1, FileIoSize.From(1, FileIoSizeUnit.Byte).Bytes);
        Assert.Equal("100 B", FileIoSize.FromBytes(100).Display);
    }

    [Fact]
    public void Size_rejects_negative_and_overflow()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => FileIoSize.From(-1, FileIoSizeUnit.MB));
        Assert.Throws<ArgumentOutOfRangeException>(() => FileIoSize.From(decimal.MaxValue, FileIoSizeUnit.TiB));
        Assert.Equal("0 B", FileIoSize.Normalize(-3));
    }

    [Fact]
    public void Buckets_match_job_watermarks()
    {
        Assert.Equal(FileIoBucket.Tiny, FileIoBuckets.For(1));
        Assert.Equal(FileIoBucket.Tiny, FileIoBuckets.For(256L * 1024));
        Assert.Equal(FileIoBucket.Small, FileIoBuckets.For(256L * 1024 + 1));
        Assert.Equal(FileIoBucket.Small, FileIoBuckets.For(4L * 1024 * 1024));
        Assert.Equal(FileIoBucket.Medium, FileIoBuckets.For(4L * 1024 * 1024 + 1));
        Assert.Equal(FileIoBucket.Medium, FileIoBuckets.For(32L * 1024 * 1024));
        Assert.Equal(FileIoBucket.Large, FileIoBuckets.For(32L * 1024 * 1024 + 1));
        Assert.Equal(FileIoBucket.Large, FileIoBuckets.For(256L * 1024 * 1024));
        Assert.Equal(FileIoBucket.Huge, FileIoBuckets.For(256L * 1024 * 1024 + 1));
    }

    [Fact]
    public void AnalyzeDirectory_counts_tiny_and_large_without_copy()
    {
        var root = Path.Combine(Path.GetTempPath(), "fio-analyze-" + Guid.NewGuid().ToString("N"));
        var nested = Path.Combine(root, "n");
        Directory.CreateDirectory(nested);
        try
        {
            for (var i = 0; i < 10; i++)
                File.WriteAllBytes(Path.Combine(nested, $"t{i}.bin"), new byte[1024]);
            File.WriteAllBytes(Path.Combine(root, "big.bin"), new byte[10 * 1024 * 1024]);

            var analysis = FileIoHelper.AnalyzeDirectory(root);
            Assert.Equal(11, analysis.FileCount);
            Assert.Equal(10 * 1024 + 10L * 1024 * 1024, analysis.TotalBytes);
            Assert.Equal(10, analysis.Buckets[(int)FileIoBucket.Tiny].FileCount);
            Assert.True(analysis.Buckets[(int)FileIoBucket.Medium].FileCount + analysis.Buckets[(int)FileIoBucket.Large].FileCount >= 1);
            Assert.True(File.Exists(Path.Combine(root, "big.bin")));
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void AnalyzeDirectory_empty_is_zeros()
    {
        var root = Path.Combine(Path.GetTempPath(), "fio-empty-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        try
        {
            var analysis = FileIoHelper.AnalyzeDirectory(root);
            Assert.Equal(0, analysis.FileCount);
            Assert.Equal(0, analysis.TotalBytes);
            Assert.Equal(0, analysis.FileSizes.Count);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void AnalyzeDirectory_filters_mask_depth_and_size()
    {
        var root = Path.Combine(Path.GetTempPath(), "fio-filter-" + Guid.NewGuid().ToString("N"));
        var skip = Path.Combine(root, "skipme");
        var keep = Path.Combine(root, "keep");
        Directory.CreateDirectory(skip);
        Directory.CreateDirectory(keep);
        try
        {
            File.WriteAllBytes(Path.Combine(root, "tiny.bin"), new byte[10]);
            File.WriteAllBytes(Path.Combine(root, "ignore.tmp"), new byte[20]);
            File.WriteAllBytes(Path.Combine(skip, "hidden.bin"), new byte[30]);
            File.WriteAllBytes(Path.Combine(keep, "ok.bin"), new byte[40]);
            var analysis = FileIoHelper.AnalyzeDirectory(root, new FileIoAnalyzeOptions
            {
                ExcludeFileMasks = ["*.tmp"],
                ExcludeDirectoryMasks = ["skipme"],
                MinSizeBytes = 15,
                MaxSizeBytes = 100,
                MaxDepth = 2
            });
            Assert.Equal(1, analysis.FileCount);
            Assert.Equal(40, analysis.TotalBytes);
        }
        finally
        {
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void AnalyzeDirectory_missing_throws()
        => Assert.Throws<DirectoryNotFoundException>(() => FileIoHelper.AnalyzeDirectory(Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid().ToString("N"))));

    [Fact]
    public void AnalyzeDirectory_blank_throws()
        => Assert.ThrowsAny<ArgumentException>(() => FileIoHelper.AnalyzeDirectory(" "));

    [Fact]
    public void WriteProbe_deletes_and_reports_rate()
    {
        var dir = Path.Combine(Path.GetTempPath(), "fio-probe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var result = FileIoHelper.WriteProbe(dir, 1, FileIoSizeUnit.MiB);
            Assert.True(result.BytesPerSecond > 0);
            Assert.Equal(1L * 1024 * 1024, result.Size.Bytes);
            Assert.True(result.Deleted);
            Assert.False(File.Exists(result.Path));
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void WriteProbe_keep_zero_and_read()
    {
        var dir = Path.Combine(Path.GetTempPath(), "fio-probe-keep-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var written = FileIoHelper.WriteProbe(dir, FileIoSize.From(8, FileIoSizeUnit.KiB), new FileIoProbeOptions
            {
                RandomBytes = false,
                KeepProbe = true,
                FileName = "kept.bin"
            });
            Assert.False(written.Deleted);
            Assert.True(File.Exists(written.Path));
            Assert.Equal(8 * 1024, new FileInfo(written.Path).Length);
            var read = FileIoHelper.ReadProbe(written.Path);
            Assert.False(read.Deleted);
            Assert.Equal(8 * 1024, read.Size.Bytes);
            Assert.True(read.BytesPerSecond > 0);
        }
        finally
        {
            Directory.Delete(dir, true);
        }
    }

    [Fact]
    public void WriteProbe_zero_bytes_and_blank_dir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "fio-probe-zero-" + Guid.NewGuid().ToString("N"));
        try
        {
            var zero = FileIoHelper.WriteProbe(dir, FileIoSize.FromBytes(0));
            Assert.True(zero.Deleted);
            Assert.ThrowsAny<ArgumentException>(() => FileIoHelper.WriteProbe(" ", FileIoSize.FromBytes(1)));
        }
        finally
        {
            if (Directory.Exists(dir))
                Directory.Delete(dir, true);
        }
    }
}
