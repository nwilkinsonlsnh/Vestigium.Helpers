using Vestigium.Helpers.FileIo;

namespace Vestigium.Helpers.Tests;

public sealed class FileIoPR04Tests
{
    [Fact]
    public async Task Unique_content_copy_writes_jsonl_and_CleanIndex_deletes_it()
    {
        var indexRoot = Path.Combine(Path.GetTempPath(), "VestigiumFileIoPR04Index", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(indexRoot);
        FileIoHelper.IndexRootOverride = indexRoot;
        var root = Path.Combine(Path.GetTempPath(), "VestigiumFileIoPR04", Guid.NewGuid().ToString("N"));
        var src = Path.Combine(root, "src");
        var dst = Path.Combine(root, "dst");
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(dst);
        File.WriteAllText(Path.Combine(src, "a.txt"), "unique-bytes");
        try
        {
            var result = await FileIoHelper.Copy(src, dst, new FileIoJobOptions
            {
                ReconLeadTime = TimeSpan.Zero,
                CopyOnlyUniqueContent = true,
            }).RunAsync();
            Assert.Equal("Success", result.Status);
            var index = FileIoHelper.IndexPath(dst);
            Assert.StartsWith(indexRoot, index, StringComparison.OrdinalIgnoreCase);
            Assert.True(File.Exists(index));
            var text = File.ReadAllText(index);
            Assert.Contains("digest", text, StringComparison.OrdinalIgnoreCase);
            Assert.Contains("a.txt", text, StringComparison.OrdinalIgnoreCase);
            FileIoHelper.CleanIndex(dst);
            Assert.False(File.Exists(index));
        }
        finally
        {
            FileIoHelper.IndexRootOverride = null;
        }
    }

    [Fact]
    public void Audit_mode_does_not_write_the_index()
    {
        var indexRoot = Path.Combine(Path.GetTempPath(), "VestigiumFileIoPR04Index", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(indexRoot);
        FileIoHelper.IndexRootOverride = indexRoot;
        var root = Path.Combine(Path.GetTempPath(), "VestigiumFileIoPR04", Guid.NewGuid().ToString("N"));
        var src = Path.Combine(root, "src");
        var dst = Path.Combine(root, "dst");
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(dst);
        File.WriteAllText(Path.Combine(src, "a.txt"), "audit");
        try
        {
            FileIoHelper.Copy(src, dst, new FileIoJobOptions
            {
                ReconLeadTime = TimeSpan.Zero,
                CopyOnlyUniqueContent = true,
                AuditMode = true,
            }).RunAsync().GetAwaiter().GetResult();
            var index = FileIoHelper.IndexPath(dst);
            Assert.False(File.Exists(index));
        }
        finally
        {
            FileIoHelper.IndexRootOverride = null;
        }
    }

    [Fact]
    public void Mask_star_tmp_does_not_match_notatmp()
    {
        Assert.True(FileIoMask.Matches("foo.tmp", ["*.tmp"]));
        Assert.False(FileIoMask.Matches("notatmp.txt", ["*.tmp"]));
        Assert.True(FileIoMask.Matches("REPORT.TMP", ["*.tmp"]));
        Assert.False(FileIoMask.Matches("a.txt", Array.Empty<string>()));
    }

    [Fact]
    public void Analyze_star_tmp_does_not_count_notatmp()
    {
        var root = Path.Combine(Path.GetTempPath(), "VestigiumFileIoPR04Analyze", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        File.WriteAllText(Path.Combine(root, "keep.txt"), "keep");
        File.WriteAllText(Path.Combine(root, "drop.tmp"), "drop");
        File.WriteAllText(Path.Combine(root, "notatmp.txt"), "keep-too");
        var analysis = FileIoHelper.AnalyzeDirectory(root, new FileIoAnalyzeOptions
        {
            ExcludeFileMasks = ["*.tmp"],
        });
        Assert.Equal(2, analysis.FileCount);
    }

    [Fact]
    public void Production_index_root_is_programdata_when_override_is_null()
    {
        FileIoHelper.IndexRootOverride = null;
        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.CommonApplicationData),
            "Vestigium", "FileIo", "Indexes");
        Assert.Equal(expected, FileIoHelper.IndexRoot());
    }
}
