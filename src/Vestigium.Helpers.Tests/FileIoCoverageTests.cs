using Vestigium.Helpers.FileIo;
using Vestigium.Helpers.Hashing;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class FileIoCoverageTests
{
    public FileIoCoverageTests()
    {
        HelperLog.Shutdown();
    }

    [Fact]
    public void Age_cutoff_prefers_date_then_days_then_min_value()
    {
        var date = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.FromHours(-5));
        var withDate = new FileIoAge { Date = date, Days = 99 };
        Assert.Equal(date.ToUniversalTime(), withDate.CutoffUtc());

        var withDays = new FileIoAge { Days = 5 };
        var daysCutoff = withDays.CutoffUtc();
        Assert.InRange(daysCutoff, DateTimeOffset.UtcNow.AddDays(-5).AddSeconds(-2), DateTimeOffset.UtcNow.AddDays(-5).AddSeconds(2));

        Assert.Equal(DateTimeOffset.MinValue, new FileIoAge().CutoffUtc());
    }

    [Fact]
    public void CompareFiles_covers_equal_different_and_missing()
    {
        var dir = TempDir();
        var left = Path.Combine(dir, "left.txt");
        var right = Path.Combine(dir, "right.txt");
        File.WriteAllText(left, "same");
        File.WriteAllText(right, "same");
        var equal = FileIoHelper.CompareFiles(left, right);
        Assert.True(equal.Equal);
        Assert.False(equal.LeftMissing);
        Assert.False(equal.RightMissing);
        Assert.False(string.IsNullOrWhiteSpace(equal.LeftDigest));
        Assert.Equal(equal.LeftDigest, equal.RightDigest);

        File.WriteAllText(right, "other");
        var different = FileIoHelper.CompareFiles(left, right, HashingAlgorithm.Sha256);
        Assert.False(different.Equal);
        Assert.NotEqual(different.LeftDigest, different.RightDigest);

        var missingRight = FileIoHelper.CompareFiles(left, Path.Combine(dir, "nope.txt"));
        Assert.False(missingRight.Equal);
        Assert.True(missingRight.RightMissing);
        Assert.False(missingRight.LeftMissing);
        Assert.Equal("", missingRight.LeftDigest);

        var missingLeft = FileIoHelper.CompareFiles(Path.Combine(dir, "missing-left.txt"), right);
        Assert.True(missingLeft.LeftMissing);
        Assert.False(missingLeft.RightMissing);

        var both = FileIoHelper.CompareFiles(Path.Combine(dir, "a"), Path.Combine(dir, "b"));
        Assert.True(both.LeftMissing);
        Assert.True(both.RightMissing);
        Assert.False(both.Equal);
    }

    [Fact]
    public void PruneEmptyDirectories_removes_nested_empties_and_keeps_files()
    {
        var root = TempDir();
        var keep = Path.Combine(root, "keep");
        Directory.CreateDirectory(keep);
        File.WriteAllText(Path.Combine(keep, "held.txt"), "x");
        var empty = Path.Combine(root, "empty", "nested");
        Directory.CreateDirectory(empty);
        var n = FileIoHelper.PruneEmptyDirectories(root);
        Assert.True(n >= 2);
        Assert.True(File.Exists(Path.Combine(keep, "held.txt")));
        Assert.False(Directory.Exists(Path.Combine(root, "empty")));
        Assert.Equal(0, FileIoHelper.PruneEmptyDirectories(Path.Combine(root, "missing")));
        Assert.Throws<ArgumentException>(() => FileIoHelper.PruneEmptyDirectories("  "));
    }

    [Fact]
    public void CleanIndex_deletes_the_destination_index_when_present()
    {
        var root = TempDir();
        FileIoHelper.IndexRootOverride = root;
        try
        {
            var dest = Path.Combine(TempDir(), "Archive");
            Directory.CreateDirectory(dest);
            FileIoHelper.CleanIndex(dest);
            var index = Path.Combine(root, HashingHelper.HashString(Path.GetFullPath(dest)) + ".jsonl");
            File.WriteAllText(index, "{}");
            Assert.True(File.Exists(index));
            FileIoHelper.CleanIndex(dest);
            Assert.False(File.Exists(index));
        }
        finally
        {
            FileIoHelper.IndexRootOverride = null;
        }
    }

    [Fact]
    public async Task Copy_honors_size_and_age_filters()
    {
        var (src, dst) = Tree();
        var tiny = Path.Combine(src, "tiny.txt");
        var large = Path.Combine(src, "large.bin");
        File.WriteAllText(tiny, "x");
        File.WriteAllBytes(large, new byte[4096]);

        await FileIoHelper.Copy(src, dst, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            MinSizeBytes = 100
        }).RunAsync();
        Assert.False(File.Exists(Path.Combine(dst, "tiny.txt")));
        Assert.True(File.Exists(Path.Combine(dst, "large.bin")));

        var (src2, dst2) = Tree();
        File.WriteAllText(Path.Combine(src2, "tiny.txt"), "x");
        File.WriteAllBytes(Path.Combine(src2, "large.bin"), new byte[4096]);
        await FileIoHelper.Copy(src2, dst2, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            MaxSizeBytes = 100
        }).RunAsync();
        Assert.True(File.Exists(Path.Combine(dst2, "tiny.txt")));
        Assert.False(File.Exists(Path.Combine(dst2, "large.bin")));

        var (src3, dst3) = Tree();
        var oldFile = Path.Combine(src3, "old.txt");
        var newFile = Path.Combine(src3, "new.txt");
        File.WriteAllText(oldFile, "old");
        File.WriteAllText(newFile, "new");
        File.SetLastWriteTimeUtc(oldFile, DateTime.UtcNow.AddDays(-10));
        File.SetLastWriteTimeUtc(newFile, DateTime.UtcNow);
        await FileIoHelper.Copy(src3, dst3, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            MinAge = new FileIoAge { Days = 5 }
        }).RunAsync();
        Assert.True(File.Exists(Path.Combine(dst3, "old.txt")));
        Assert.False(File.Exists(Path.Combine(dst3, "new.txt")));

        var (src4, dst4) = Tree();
        File.WriteAllText(Path.Combine(src4, "old.txt"), "old");
        File.WriteAllText(Path.Combine(src4, "new.txt"), "new");
        File.SetLastWriteTimeUtc(Path.Combine(src4, "old.txt"), DateTime.UtcNow.AddDays(-10));
        File.SetLastWriteTimeUtc(Path.Combine(src4, "new.txt"), DateTime.UtcNow);
        await FileIoHelper.Copy(src4, dst4, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            MaxAge = new FileIoAge { Days = 5 }
        }).RunAsync();
        Assert.False(File.Exists(Path.Combine(dst4, "old.txt")));
        Assert.True(File.Exists(Path.Combine(dst4, "new.txt")));
    }

    [Fact]
    public async Task Copy_min_age_date_cutoff_skips_newer_files()
    {
        var (src, dst) = Tree();
        var oldFile = Path.Combine(src, "old.txt");
        var newFile = Path.Combine(src, "new.txt");
        File.WriteAllText(oldFile, "old");
        File.WriteAllText(newFile, "new");
        File.SetLastWriteTimeUtc(oldFile, DateTime.UtcNow.AddDays(-10));
        File.SetLastWriteTimeUtc(newFile, DateTime.UtcNow);
        var cutoff = DateTimeOffset.UtcNow.AddDays(-3);
        await FileIoHelper.Copy(src, dst, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            MinAge = new FileIoAge { Date = cutoff, Days = 1 }
        }).RunAsync();
        Assert.True(File.Exists(Path.Combine(dst, "old.txt")));
        Assert.False(File.Exists(Path.Combine(dst, "new.txt")));
    }

    [Fact]
    public void Shred_recipe_tostring_and_secure_delete()
    {
        Assert.Equal("Zero,Random,Zero", FileIoShredRecipe.ZeroRandomZero.ToString());
        Assert.Equal("Random,Random,Random,Zero", FileIoShredRecipe.ThreeRandomThenZero.ToString());
        Assert.Equal("Random,Random,Random,Random,Random,Random,Random,Zero", FileIoShredRecipe.SevenRandomThenZero.ToString());
        var path = Path.Combine(TempDir(), "shred.bin");
        File.WriteAllBytes(path, [1, 2, 3, 4]);
        FileIoHelper.SecureDelete(path, FileIoShredRecipe.ZeroRandomZero);
        Assert.False(File.Exists(path));
        Assert.Throws<FileNotFoundException>(() => FileIoHelper.SecureDelete(path, FileIoShredRecipe.ZeroRandomZero));
    }

    [Fact]
    public void CleanIndexesOlderThan_missing_root_is_a_no_op()
    {
        FileIoHelper.IndexRootOverride = Path.Combine(Path.GetTempPath(), "VestigiumFileIoMissing", Guid.NewGuid().ToString("N"));
        try
        {
            FileIoHelper.CleanIndexesOlderThan(TimeSpan.FromDays(1));
        }
        finally
        {
            FileIoHelper.IndexRootOverride = null;
        }
    }

    [Fact]
    public async Task Prune_empty_directories_option_runs_after_copy()
    {
        var (src, dst) = Tree();
        Directory.CreateDirectory(Path.Combine(dst, "leftover", "nested"));
        File.WriteAllText(Path.Combine(src, "a.txt"), "x");
        var result = await FileIoHelper.Copy(src, dst, new FileIoJobOptions
        {
            ReconLeadTime = TimeSpan.Zero,
            PruneEmptyDirectories = true
        }).RunAsync();
        Assert.Equal("Success", result.Status);
        Assert.False(Directory.Exists(Path.Combine(dst, "leftover")));
    }

    [Fact]
    public void UniqueName_parse_and_bucket_for()
    {
        Assert.Equal(new UniqueName.Spec(false, 2), UniqueName.Parse(".##"));
        Assert.Equal(new UniqueName.Spec(true, 3), UniqueName.Parse("A###"));
        Assert.Throws<ArgumentException>(() => UniqueName.Parse("##"));
        Assert.Throws<ArgumentException>(() => UniqueName.Parse(".######"));
        Assert.Equal(FileIoBucket.Tiny, FileIoJob.BucketFor(0));
        Assert.Equal(FileIoBucket.Tiny, FileIoJob.BucketFor(256 * 1024));
        Assert.Equal(FileIoBucket.Small, FileIoJob.BucketFor(256 * 1024 + 1));
        Assert.Equal(FileIoBucket.Medium, FileIoJob.BucketFor(4 * 1024 * 1024 + 1));
        Assert.Equal(FileIoBucket.Large, FileIoJob.BucketFor(32 * 1024 * 1024 + 1));
        Assert.Equal(FileIoBucket.Huge, FileIoJob.BucketFor(256 * 1024 * 1024 + 1));
    }

    static (string Src, string Dst) Tree()
    {
        var root = TempDir();
        var src = Path.Combine(root, "Export");
        var dst = Path.Combine(root, "Archive");
        Directory.CreateDirectory(src);
        Directory.CreateDirectory(dst);
        return (src, dst);
    }

    static string TempDir()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumFileIoTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        return dir;
    }
}
