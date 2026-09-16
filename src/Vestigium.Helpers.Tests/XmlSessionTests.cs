using System.Xml;
using System.Xml.Linq;
using Vestigium.Helpers.Tests.Support;
using Vestigium.Helpers.Xml;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class XmlSessionTests
{
    public XmlSessionTests()
    {
        HelperLog.Shutdown();
        var area = Path.Combine(Path.GetTempPath(), "VestigiumXmlTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(area);
        Environment.SetEnvironmentVariable("VESTIGIUM_XML_TESTAREA", area);
        XmlTestHooks.ExportRoot = Path.Combine(area, "exports");
        XmlContentSeeder.ResetForTests();
        XmlContentSeeder.EnsureSeeded();
    }

    [Fact]
    public void Identity_is_stable()
    {
        Assert.Equal("Vestigium.Helpers.Xml", XmlHelper.Identity);
    }

    [Fact]
    public void Probe_parses_in_memory_and_stays_in_temp()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumXmlTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        HelperLog.InitializeHost(HelperLog.AppIds.Xml, cfg => cfg.LogDirectory = dir);
        try
        {
            Assert.Equal(XmlHelper.Identity, XmlHelper.Probe());
            Assert.StartsWith(Path.GetTempPath().TrimEnd(Path.DirectorySeparatorChar), dir);
            var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
            Assert.DoesNotContain(dir, desktop);
        }
        finally
        {
            HelperLog.Shutdown();
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void Osinfo_snapshot_set_diff_commit_save_reload()
    {
        var path = XmlContentSeeder.CreateScratchCopy("osinfo.xml");
        using (var doc = XmlHelper.Open(path))
        {
            doc.Snapshot();
            doc.SetText("//u:action/u:name", "MagicOff");
            var changes = doc.Diff();
            Assert.Contains(changes, c => c.Op == "set-text");
            Assert.True(doc.HasUncommittedWork);
            doc.Commit();
            Assert.False(doc.HasUncommittedWork);
            Assert.True(doc.HasUnsavedCommit);
            doc.Save();
        }

        using var reloaded = XmlHelper.Open(path);
        Assert.Equal("MagicOff", reloaded.First(XmlSearch.ByText("MagicOff"))!.Text);
        Assert.Null(reloaded.First(XmlSearch.ByText("MagicOn")));
    }

    [Fact]
    public void Cancel_after_set_leaves_disk_untouched()
    {
        var path = XmlContentSeeder.CreateScratchCopy("osinfo.xml");
        var before = File.ReadAllBytes(path);
        using (var doc = XmlHelper.Open(path))
        {
            doc.SetText("//u:action/u:name", "MagicOff");
            doc.Cancel();
            Assert.False(doc.HasUncommittedWork);
        }

        Assert.Equal(before, File.ReadAllBytes(path));
    }

    [Fact]
    public void ReAgent_set_attribute_round_trips()
    {
        var path = XmlContentSeeder.CreateScratchCopy("ReAgent.xml");
        using (var doc = XmlHelper.Open(path))
        {
            doc.Snapshot();
            doc.SetAttribute("/WindowsRE/InstallState", "state", "0");
            Assert.Contains(doc.Diff(), c => c.Op == "set-attribute");
            doc.Commit();
            doc.Save();
        }

        using var reloaded = XmlHelper.Open(path);
        var node = reloaded.First(XmlSearch.ByName("InstallState"));
        Assert.NotNull(node);
        Assert.Equal("0", node!.Attributes["state"]);
    }

    [Fact]
    public void Ipcfg_xpath_needs_default_xmlns_bind()
    {
        using var doc = XmlHelper.Open(XmlContentSeeder.GetPath("ipcfg.xml"));
        var names = doc.Search(XmlSearch.XPath("//u:action/u:name"));
        Assert.NotEmpty(names);
        Assert.Contains(names, n => n.Text == "SetConnectionType");
        Assert.NotEmpty(doc.Search(XmlSearch.ByName("action")));
    }

    [Fact]
    public void Create_insert_commit_saveas_collision()
    {
        var dest = Path.Combine(XmlTestHooks.ExportRoot!, "probe-settings.xml");
        using var doc = XmlHelper.Create();
        doc.Insert("/", new XElement("root", new XElement("appId", "PingIQ")));
        doc.Commit();
        var saved = doc.SaveAs(dest);
        Assert.True(File.Exists(saved));
        Assert.Throws<IOException>(() => { doc.SaveAs(dest); });
        doc.SaveAs(dest, XmlCollision.Overwrite);
    }

    [Fact]
    public void Delete_root_is_refused()
    {
        using var doc = XmlHelper.Open(XmlContentSeeder.GetPath("osinfo.xml"));
        Assert.Throws<InvalidOperationException>(() => { doc.Delete("/u:scpd"); });
    }

    [Fact]
    public void Session_logs_paths_and_op_counts_never_bodies()
    {
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumXmlTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        HelperLog.InitializeHost(HelperLog.AppIds.Xml, cfg => cfg.LogDirectory = dir);
        try
        {
            const string secret = "hunter2-xml-must-not-log";
            var path = XmlContentSeeder.CreateScratchCopy("osinfo.xml");
            using var doc = XmlHelper.Open(path);
            doc.Snapshot();
            doc.SetText("//u:action/u:name", secret);
            var changes = doc.Diff();
            Assert.NotEmpty(changes);
            doc.Commit();
            doc.Cancel();

            var lines = HelperLog.RecentJsonLines;
            Assert.DoesNotContain(lines, l => l.Contains(secret));
            Assert.Contains(lines, l => l.Contains("\"SUBCATEGORY\":\"Diff\"") && l.Contains("ops="));
            Assert.Contains(lines, l => l.Contains("\"SUBCATEGORY\":\"Snapshot\""));
            Assert.All(lines, line => Assert.DoesNotContain("\"EXCEPTION\":\"", line.Replace("\"EXCEPTION\":null", "")));
        }
        finally
        {
            HelperLog.Shutdown();
            Directory.Delete(dir, recursive: true);
        }
    }
}
