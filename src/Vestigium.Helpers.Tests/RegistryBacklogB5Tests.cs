using System.Diagnostics;
using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class RegistryBacklogB5Tests
{
    [Fact]
    public void Privilege_scope_dispose_does_not_throw()
    {
        using var scope = RegistryNative.BackupRestore();
        Assert.True(true);
    }

    [Fact]
    public void CanConnect_local_is_true()
        => Assert.True(RegistryHelper.CanConnect("."));

    [Fact]
    public void CanConnect_missing_host_returns_false_quickly()
    {
        var previous = RegistryHelper.ConnectTimeout;
        RegistryHelper.ConnectTimeout = TimeSpan.FromSeconds(2);
        try
        {
            var clock = Stopwatch.StartNew();
            var ok = RegistryHelper.CanConnect("no-such-host-vestigium-b5", out var reason);
            clock.Stop();
            Assert.False(ok);
            Assert.False(string.IsNullOrWhiteSpace(reason));
            Assert.True(clock.Elapsed < TimeSpan.FromSeconds(8), clock.Elapsed.ToString());
        }
        finally
        {
            RegistryHelper.ConnectTimeout = previous;
        }
    }
}
