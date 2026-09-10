using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ProcessHelperTests
{
    [Fact]
    public void Identity_is_stable()
        => Assert.Equal("Vestigium.Helpers.Processes", ProcessHelper.Identity);

    [Fact]
    public void List_includes_current_process()
        => Assert.Contains(ProcessHelper.List(), row => row.Pid == Environment.ProcessId);

    [Fact]
    public void Get_current_process_has_name_and_image()
    {
        var row = ProcessHelper.Get(Environment.ProcessId);
        Assert.NotNull(row);
        Assert.Equal(Environment.ProcessId, row.Pid);
        Assert.False(string.IsNullOrWhiteSpace(row.Name));
        Assert.False(string.IsNullOrWhiteSpace(row.ImagePath));
    }

    [Fact]
    public void Get_full_row_sets_image_type()
    {
        var row = ProcessHelper.Get(Environment.ProcessId);
        Assert.NotNull(row);
        Assert.NotEqual(ProcessImageType.Unknown, row.ImageType);
        Assert.NotNull(row.VerifiedSigner);
        Assert.True(row.VerifiedSigner.Value.Trust is SignerTrust.Verified or SignerTrust.NotSigned or SignerTrust.Untrusted or SignerTrust.Expired or SignerTrust.Denied or SignerTrust.Unknown);
    }

    [Fact]
    public void SetComment_round_trips_on_injected_store()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumProcessTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        ProcessTestHooks.CommentStorePath = Path.Combine(dir, "comments.json");
        try
        {
            ProcessHelper.SetComment(Environment.ProcessId, "phase-2", persist: true);
            var row = ProcessHelper.Get(Environment.ProcessId);
            Assert.Equal("phase-2", row?.Comment);
            Assert.True(File.Exists(ProcessTestHooks.CommentStorePath));
        }
        finally
        {
            ProcessTestHooks.CommentStorePath = null;
            try { Directory.Delete(dir, true); } catch { }
        }
    }

    [Fact]
    public void Get_unknown_pid_returns_null()
        => Assert.Null(ProcessHelper.Get(int.MaxValue - 7));

    [Fact]
    public void Get_rejects_non_positive_pid()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProcessHelper.Get(0));
        Assert.Throws<ArgumentOutOfRangeException>(() => ProcessHelper.Get(-1));
    }

    [Fact]
    public void TryGet_current_process()
    {
        Assert.True(ProcessHelper.TryGet(Environment.ProcessId, out var row));
        Assert.NotNull(row);
        Assert.Equal(Environment.ProcessId, row.Pid);
        Assert.False(ProcessHelper.TryGet(int.MaxValue - 7, out var missing));
        Assert.Null(missing);
    }

    [Fact]
    public void Search_starts_with_current_name()
    {
        var self = MustSelf();
        var hits = ProcessHelper.Search(Prefix(self.Name, 3), ProcessSearchMode.StartsWith, ProcessSearchFields.Name);
        Assert.Contains(hits, row => row.Pid == self.Pid);
    }

    [Fact]
    public void Search_ends_with_current_name()
    {
        var self = MustSelf();
        // Full name: a 3-char suffix of testhost.exe is "exe" and hits the 256 cap before this PID.
        var hits = ProcessHelper.Search(self.Name, ProcessSearchMode.EndsWith, ProcessSearchFields.Name);
        Assert.Contains(hits, row => row.Pid == self.Pid);
    }

    [Fact]
    public void Search_contains_current_name()
    {
        var self = MustSelf();
        var hits = ProcessHelper.Search(Token(self.Name), ProcessSearchMode.Contains, ProcessSearchFields.Name);
        Assert.Contains(hits, row => row.Pid == self.Pid);
    }

    [Fact]
    public void Search_rejects_blank_term()
    {
        Assert.Throws<ArgumentException>(() => ProcessHelper.Search("  ", ProcessSearchMode.Contains));
        Assert.Throws<ArgumentException>(() => ProcessHelper.Search("", ProcessSearchMode.StartsWith));
    }

    [Fact]
    public void Search_rejects_out_of_range_max()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => ProcessHelper.Search("a", ProcessSearchMode.Contains, maxResults: 0));
        Assert.Throws<ArgumentException>(() => ProcessHelper.Search("a", ProcessSearchMode.Contains, maxResults: 4097));
    }

    private static ProcessInfo MustSelf()
    {
        var row = ProcessHelper.Get(Environment.ProcessId);
        Assert.NotNull(row);
        return row;
    }

    private static string Prefix(string name, int length) => name[..Math.Min(length, name.Length)];
    private static string Token(string name)
    {
        var stem = Path.GetFileNameWithoutExtension(name);
        return stem.Length >= 3 ? stem[1..Math.Min(4, stem.Length)] : name[..1];
    }
}
