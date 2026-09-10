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
    {
        var rows = ProcessHelper.List();
        Assert.Contains(rows, row => row.Pid == Environment.ProcessId);
    }

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
        var prefix = Prefix(self.Name, 3);
        var hits = ProcessHelper.Search(prefix, ProcessSearchMode.StartsWith, ProcessSearchFields.Name);
        Assert.Contains(hits, row => row.Pid == self.Pid);
    }

    [Fact]
    public void Search_ends_with_current_name()
    {
        var self = MustSelf();
        var suffix = Suffix(self.Name, 3);
        var hits = ProcessHelper.Search(suffix, ProcessSearchMode.EndsWith, ProcessSearchFields.Name);
        Assert.Contains(hits, row => row.Pid == self.Pid);
    }

    [Fact]
    public void Search_contains_current_name()
    {
        var self = MustSelf();
        var token = Token(self.Name);
        var hits = ProcessHelper.Search(token, ProcessSearchMode.Contains, ProcessSearchFields.Name);
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
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            ProcessHelper.Search("a", ProcessSearchMode.Contains, maxResults: 0));
        Assert.Throws<ArgumentException>(() =>
            ProcessHelper.Search("a", ProcessSearchMode.Contains, maxResults: 4097));
    }

    private static ProcessInfo MustSelf()
    {
        var row = ProcessHelper.Get(Environment.ProcessId);
        Assert.NotNull(row);
        return row;
    }

    private static string Prefix(string name, int length)
        => name[..Math.Min(length, name.Length)];

    private static string Suffix(string name, int length)
        => name[^Math.Min(length, name.Length)..];

    private static string Token(string name)
    {
        var stem = Path.GetFileNameWithoutExtension(name);
        if (stem.Length >= 3)
            return stem[1..Math.Min(4, stem.Length)];
        return name[..1];
    }
}
