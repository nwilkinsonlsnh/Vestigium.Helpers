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
    public void AnalyzeDirectory_missing_throws()
        => Assert.Throws<DirectoryNotFoundException>(() => FileIoHelper.AnalyzeDirectory(Path.Combine(Path.GetTempPath(), "missing-" + Guid.NewGuid().ToString("N"))));

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
}
