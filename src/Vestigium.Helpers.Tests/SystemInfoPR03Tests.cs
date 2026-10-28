using Vestigium.Helpers.SystemInfo;

namespace Vestigium.Helpers.Tests;

public sealed class SystemInfoPR03Tests
{
    [Fact]
    public void Security_records_hold_raw_cim_values()
    {
        var profile = new FirewallProfile("Domain", 1, 2, 2, true, false, true);
        Assert.Equal(1u, profile.Enabled);
        Assert.Equal(2u, profile.DefaultInboundAction);
        Assert.True(profile.NotifyOnListen);

        var rule = new FirewallRule("Allow ICMP", 1, 1, 2, 7, "1");
        Assert.Equal(7u, rule.Profiles);
        Assert.Equal(1u, rule.Direction);

        var cert = new CertificateInfo("LM", "Root", "CN=Test", "CN=Test", "ABC", DateTime.UtcNow, false);
        Assert.Equal("LM", cert.Location);
        Assert.False(cert.HasPrivateKey);

        var driver = new DriverInfo("PnP", "Disk", "1.0", "Acme", "True", "Acme", "disk.inf");
        Assert.Equal("PnP", driver.Source);
    }

    [Fact]
    public void Software_records_hold_raw_values()
    {
        var installed = new InstalledSoftware("LM", "x64", "App", "1.0", "Acme", "20260101", true);
        Assert.True(installed.Hidden);
        Assert.Equal("x64", installed.Arch);

        var feature = new OptionalFeature("NetFx3", 1);
        Assert.Equal(1u, feature.InstallState);

        var sql = new SqlService("MSSQLSERVER", "SQL Server", 4, 2);
        Assert.Equal(4u, sql.Status);
        Assert.Equal(2u, sql.StartType);

        var env = new EnvironmentEntry("Machine", "PATH", "C:\\Windows");
        Assert.Equal("Machine", env.Scope);
    }

    [Fact]
    public void Unavailable_has_no_value()
    {
        var fact = Fact<IReadOnlyList<FirewallRule>>.Unavailable();
        Assert.False(fact.IsOk);
        Assert.Equal(FactStatus.Unavailable, fact.Status);
        Assert.Throws<InvalidOperationException>(() => _ = fact.Value);
    }

    [Fact]
    public void Inventory_doors_exist()
    {
        Assert.NotNull(typeof(SystemInventory).GetMethod(nameof(SystemInventory.ReadBase)));
        Assert.NotNull(typeof(SystemInventory).GetMethod(nameof(SystemInventory.ReadStorage)));
        Assert.NotNull(typeof(SystemInventory).GetMethod(nameof(SystemInventory.ReadNetwork)));
        Assert.NotNull(typeof(SystemInventory).GetMethod(nameof(SystemInventory.ReadSecurity)));
        Assert.NotNull(typeof(SystemInventory).GetMethod(nameof(SystemInventory.ReadSoftware)));
    }
}
