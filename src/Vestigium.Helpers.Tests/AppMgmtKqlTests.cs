using Vestigium.Helpers.Kql;
using Vestigium.Helpers.Kql.AppMgmt;

namespace Vestigium.Helpers.Tests;

public sealed class AppMgmtKqlTests
{
    [Fact]
    public void Sessions_Open_From_The_External_Door()
    {
        using var packages = AppMgmtKql.Packages();
        using var features = AppMgmtKql.Features();
        using var startup = AppMgmtKql.Startup();
        using var footprints = AppMgmtKql.Footprints();

        Assert.Empty(packages.Packs);
        Assert.Empty(features.Packs);
        Assert.Empty(startup.Packs);
        Assert.Empty(footprints.Packs);
        Assert.Equal(18, packages.Fields.Count);
        Assert.Equal(5, features.Fields.Count);
        Assert.Equal(8, startup.Fields.Count);
        Assert.Equal(6, footprints.Fields.Count);
    }

    [Theory]
    [InlineData("app.displayname LIKE '%chrome%'")]
    [InlineData("displayname LIKE '%office%'")]
    [InlineData("app.publisher LIKE '%Microsoft%'")]
    [InlineData("app.version == '1.2.3'")]
    [InlineData("app.MajorVersion GE 10")]
    [InlineData("app.MinorVersion == 0")]
    [InlineData("app.InstallDate LIKE '%2026%'")]
    [InlineData("app.size.mb GT 100")]
    [InlineData("app.size.kb GT 51200")]
    [InlineData("app.size.gb GT 1")]
    [InlineData("app.IsX64 == true")]
    [InlineData("app.IsX86 == true")]
    [InlineData("app.IsGlobal == true")]
    [InlineData("app.IsSystemComponent == false")]
    [InlineData("app.WindowsInstaller == true")]
    [InlineData("app.NoModify == true")]
    [InlineData("app.NoRemove == true")]
    [InlineData("app.NoRepair == false")]
    [InlineData("app.HasIcon == true")]
    [InlineData("app.publisher LIKE '%Microsoft%' && app.IsX64 == true && app.size.mb GT 50")]
    public void Package_Accepts_Compile(string text)
    {
        using var session = AppMgmtKql.Packages();
        var compiled = KqlHelper.Compile(text, session);
        Assert.True(compiled.Ok, compiled.Error?.Message);
    }

    [Theory]
    [InlineData("feature.state == Enabled")]
    [InlineData("feature.caption LIKE '%Hyper%'")]
    [InlineData("feature.parent == 'NetFx4-AdvSrvs'")]
    [InlineData("feature.IsEnabled == true")]
    [InlineData("state == Disabled")]
    public void Feature_Accepts_Compile(string text)
    {
        using var session = AppMgmtKql.Features();
        var compiled = KqlHelper.Compile(text, session);
        Assert.True(compiled.Ok, compiled.Error?.Message);
    }

    [Theory]
    [InlineData("startup.name LIKE '%OneDrive%'")]
    [InlineData("startup.command LIKE '%exe%'")]
    [InlineData("startup.source == 'Run'")]
    [InlineData("startup.scope == 'HKLM'")]
    [InlineData("startup.isX64 == true")]
    [InlineData("startup.isX86 == true")]
    [InlineData("startup.package LIKE '%Chrome%'")]
    [InlineData("startup.enabled == false")]
    [InlineData("name LIKE '%Teams%'")]
    public void Startup_Accepts_Compile(string text)
    {
        using var session = AppMgmtKql.Startup();
        var compiled = KqlHelper.Compile(text, session);
        Assert.True(compiled.Ok, compiled.Error?.Message);
    }

    [Theory]
    [InlineData("footprint.kind == 'AppPath'")]
    [InlineData("footprint.name LIKE '%chrome%'")]
    [InlineData("footprint.path LIKE '%Program Files%'")]
    [InlineData("footprint.scope == 'HKLM'")]
    [InlineData("footprint.refcount GT 1")]
    [InlineData("footprint.package LIKE '%Office%'")]
    [InlineData("kind == 'SharedDll'")]
    public void Footprints_Accepts_Compile(string text)
    {
        using var session = AppMgmtKql.Footprints();
        var compiled = KqlHelper.Compile(text, session);
        Assert.True(compiled.Ok, compiled.Error?.Message);
    }

    [Theory]
    [InlineData("app.size GT 100")]
    [InlineData("feature.state == Enabled")]
    [InlineData("app.IsX86 == app.IsX64")]
    public void Package_Rejects(string text)
    {
        using var session = AppMgmtKql.Packages();
        Assert.False(KqlHelper.Compile(text, session).Ok);
    }

    [Theory]
    [InlineData("app.publisher LIKE '%Microsoft%'")]
    [InlineData("feature.state == Installed")]
    public void Feature_Rejects(string text)
    {
        using var session = AppMgmtKql.Features();
        Assert.False(KqlHelper.Compile(text, session).Ok);
    }

    [Theory]
    [InlineData("app.displayname LIKE '%chrome%'")]
    [InlineData("startup.missing == true")]
    public void Startup_Rejects(string text)
    {
        using var session = AppMgmtKql.Startup();
        Assert.False(KqlHelper.Compile(text, session).Ok);
    }

    [Theory]
    [InlineData("startup.name LIKE '%OneDrive%'")]
    [InlineData("footprint.missing == true")]
    public void Footprints_Rejects(string text)
    {
        using var session = AppMgmtKql.Footprints();
        Assert.False(KqlHelper.Compile(text, session).Ok);
    }

    [Fact]
    public void DisplayName_Case_And_Suffix_Bind_The_Same_Field()
    {
        using var session = AppMgmtKql.Packages();
        Assert.True(session.TryGetField("app.displayname", out var dotted));
        Assert.True(session.TryGetField("app.DisplayName", out var mixed));
        Assert.True(session.TryGetField("displayname", out var bare));
        Assert.Same(dotted, mixed);
        Assert.Same(dotted, bare);
    }

    [Fact]
    public void Blank_Publisher_Does_Not_Match_Empty()
    {
        using var session = AppMgmtKql.Packages();
        var compiled = KqlHelper.Compile("app.publisher == ''", session);
        Assert.True(compiled.Ok, compiled.Error?.Message);

        var row = PackageAdapter.ToRow(session, new PackageFacts { Publisher = "  " });
        Assert.False(compiled.Query!.Matches(row));
        Assert.True(row.Get("app.publisher").IsUnknown);
    }

    [Fact]
    public void Missing_Size_Does_Not_Match_Greater_Than_Zero()
    {
        using var session = AppMgmtKql.Packages();
        var compiled = KqlHelper.Compile("app.size.mb GT 0", session);
        var row = PackageAdapter.ToRow(session, new PackageFacts());
        Assert.False(compiled.Query!.Matches(row));
        Assert.True(row.Get("app.size.mb").IsUnknown);
    }

    [Fact]
    public void Size_Projects_From_Kilobytes()
    {
        using var session = AppMgmtKql.Packages();
        var row = PackageAdapter.ToRow(session, new PackageFacts { EstimatedSizeKb = 102400 });
        Assert.Equal(102400d, row.Get("app.size.kb").Raw);
        Assert.Equal(100d, row.Get("app.size.mb").Raw);
    }

    [Fact]
    public void IsX86_Is_The_Inverse_Of_IsX64()
    {
        using var session = AppMgmtKql.Packages();
        var row = PackageAdapter.ToRow(session, new PackageFacts { IsX64 = false });
        var compiled = KqlHelper.Compile("app.IsX86 == true", session);
        Assert.True(compiled.Query!.Matches(row));
    }

    [Fact]
    public void Startup_Folder_Leaves_Bit_Unknown()
    {
        using var session = AppMgmtKql.Startup();
        var row = StartupAdapter.ToRow(session, new StartupFacts { Source = "Startup folder", Scope = "User" });
        Assert.True(row.Get("startup.isX64").IsUnknown);
        Assert.True(row.Get("startup.isX86").IsUnknown);
    }

    [Fact]
    public void Startup_Bit_Is_The_Inverse()
    {
        using var session = AppMgmtKql.Startup();
        var row = StartupAdapter.ToRow(session, new StartupFacts { IsX64 = false });
        var compiled = KqlHelper.Compile("startup.isX86 == true", session);
        Assert.True(compiled.Query!.Matches(row));
    }

    [Fact]
    public void Startup_Enabled_Defaults_True()
    {
        using var session = AppMgmtKql.Startup();
        var row = StartupAdapter.ToRow(session, new StartupFacts());
        Assert.Equal(true, row.Get("startup.enabled").Raw);
    }

    [Fact]
    public void Footprints_Missing_RefCount_Is_Unknown()
    {
        using var session = AppMgmtKql.Footprints();
        var row = FootprintsAdapter.ToRow(session, new FootprintsFacts { Kind = "AppPath" });
        Assert.True(row.Get("footprint.refcount").IsUnknown);
    }

    [Fact]
    public void Feature_State_Offers_Closed_Values_Only()
    {
        using var session = AppMgmtKql.Features();
        var text = "feature.state == ";
        var completion = KqlHelper.Complete(text, text.Length, session);
        var inserts = completion.Rows.Select(row => row.Insert).ToArray();
        Assert.Contains(inserts, value => value.Contains("Enabled", StringComparison.OrdinalIgnoreCase));
        Assert.DoesNotContain(inserts, value => value.Contains("Installed", StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public void Core_Process_Pack_Still_Compiles()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        Assert.True(KqlHelper.Compile("PID == 0", session).Ok);
    }
}
