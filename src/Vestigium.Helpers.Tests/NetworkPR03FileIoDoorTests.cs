using Vestigium.Helpers.FileIo;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPR03FileIoDoorTests
{
    [Fact]
    public void PR03_001_analyze_directory_matches_v15()
    {
        var root = Path.Combine(Path.GetTempPath(), "pr03-analyze-" + Guid.NewGuid().ToString("N"));
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
    public void PR03_001_write_probe_deletes()
    {
        var dir = Path.Combine(Path.GetTempPath(), "pr03-probe-" + Guid.NewGuid().ToString("N"));
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
    public void PR03_001_missing_directory_is_typed()
        => Assert.Throws<DirectoryNotFoundException>(() =>
            FileIoHelper.AnalyzeDirectory(Path.Combine(Path.GetTempPath(), "pr03-missing-" + Guid.NewGuid().ToString("N"))));
}
