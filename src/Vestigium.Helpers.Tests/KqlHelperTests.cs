using Vestigium.Helpers;
using Vestigium.Helpers.Kql;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class KqlHelperTests
{
    [Fact]
    public void Identity_is_stable()
        => Assert.Equal("Vestigium.Helpers.Kql", KqlHelper.Identity);

    [Fact]
    public void Kql_appid_is_registered()
        => Assert.True(HelperLog.Taxonomy.IsSubcategoryRegistered(HelperLog.Category, HelperLog.AppIds.Kql));

    [Fact]
    public void Process_pack_exposes_identity_and_resource_fields()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        Assert.True(session.TryGetField("PID", out var pid));
        Assert.Equal("PROC.Pid", pid.Canonical);
        Assert.True(session.TryGetField("CPU.Usage", out _));
        Assert.True(session.TryGetField("MEM.PrivateBytes", out _));
        Assert.True(session.TryGetField("GPU.Usage", out _));
        Assert.True(session.TryGetField("IO.Reads", out _));
        Assert.True(session.TryGetField("Cmd", out var cmdAlias));
        Assert.Equal("PROC.CommandLine", cmdAlias.Canonical);
        Assert.True(session.TryGetField("CommandLine", out var commandLine));
        Assert.Equal("PROC.CommandLine", commandLine.Canonical);
        Assert.True(session.TryGetField("WindowTitle", out var title));
        Assert.Equal("PROC.WindowTitle", title.Canonical);
        Assert.False(session.TryGetField("NET.Connections", out _));
        Assert.False(session.TryGetField("MEM.CommitLimit", out _));
    }

    [Fact]
    public void Cpu_private_bytes_is_alias_of_mem_private_bytes()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        Assert.True(session.TryGetField("CPU.PrivateBytes", out var cpu));
        Assert.True(session.TryGetField("MEM.PrivateBytes", out var mem));
        Assert.True(session.TryGetField("RAM.PrivateBytes", out var ram));
        Assert.Same(mem, cpu);
        Assert.Same(mem, ram);
        Assert.Equal("MEM.PrivateBytes", cpu.Canonical);
    }

    [Fact]
    public void Service_pack_does_not_expose_gpu_usage()
    {
        using var session = KqlHelper.Create(KqlPack.Service);
        Assert.True(session.TryGetField("SVC.Name", out _));
        Assert.True(session.TryGetField("Name", out var name));
        Assert.Equal("SVC.Name", name.Canonical);
        Assert.False(session.TryGetField("GPU.Usage", out _));
        Assert.False(session.TryGetField("MEM.PrivateBytes", out _));
    }

    [Fact]
    public void System_pack_exposes_commit_and_not_command_line()
    {
        using var session = KqlHelper.Create(KqlPack.System);
        Assert.True(session.TryGetField("MEM.CommitLimit", out _));
        Assert.True(session.TryGetField("CPU.ContextSwitchDelta", out _));
        Assert.False(session.TryGetField("PROC.CommandLine", out _));
        Assert.False(session.TryGetField("Cmd", out _));
    }

    [Fact]
    public void Field_lookup_is_case_insensitive()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        Assert.True(session.TryGetField("pid", out var a));
        Assert.True(session.TryGetField("Pid", out var b));
        Assert.Same(a, b);
    }
}
