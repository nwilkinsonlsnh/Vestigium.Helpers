using System.Xml;
using Vestigium.Helpers.Tests.Support;
using Vestigium.Helpers.Xml;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class XmlSafetyTests
{
    public XmlSafetyTests()
    {
        var area = Path.Combine(Path.GetTempPath(), "VestigiumXmlTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(area);
        Environment.SetEnvironmentVariable("VESTIGIUM_XML_TESTAREA", area);
        XmlTestHooks.ExportRoot = Path.Combine(area, "exports");
        XmlContentSeeder.ResetForTests();
        XmlContentSeeder.EnsureSeeded();
        XmlTestHooks.ResolverAsks = 0;
    }

    [Fact]
    public void Ddf_with_remote_dtd_does_not_ask_resolver()
    {
        XmlTestHooks.ResolverAsks = 0;
        using var doc = XmlHelper.Open(XmlContentSeeder.GetPath("DevicePreparationDDF.xml"));
        Assert.Equal("MgmtTree", doc.Search(XmlSearch.ByName("MgmtTree"))[0].LocalName);
        Assert.Equal(0, XmlTestHooks.ResolverAsks);
        Assert.False(string.IsNullOrWhiteSpace(doc.Doctype));
    }

    [Fact]
    public void External_entity_is_not_expanded()
    {
        var sentinel = Path.Combine(XmlTestHooks.ExportRoot!, "secret.txt");
        Directory.CreateDirectory(XmlTestHooks.ExportRoot!);
        File.WriteAllText(sentinel, "SHOULD-NOT-BE-READ");
        var xml = $"""
            <?xml version="1.0"?>
            <!DOCTYPE root [
              <!ENTITY xxe SYSTEM "{sentinel}">
            ]>
            <root>&xxe;</root>
            """;
        var path = Path.Combine(XmlTestHooks.ExportRoot!, "xxe.xml");
        File.WriteAllText(path, xml);
        using var doc = XmlHelper.Open(path);
        var text = doc.First(XmlSearch.ByName("root"))?.Text ?? string.Empty;
        Assert.DoesNotContain("SHOULD-NOT-BE-READ", text);
        Assert.Equal(0, XmlTestHooks.ResolverAsks);
    }

    [Fact]
    public void Size_cap_rejects_huge_input()
    {
        Assert.Throws<XmlException>(() =>
            XmlHelper.Parse("<root/>", new XmlReadOptions { MaxCharacters = 1 }));
    }

    [Fact]
    public void Default_export_directory_is_injected_in_tests()
    {
        var dir = XmlHelper.DefaultExportDirectory();
        Assert.StartsWith(XmlTestHooks.ExportRoot!, dir);
        var desktop = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
        Assert.False(dir.StartsWith(desktop, StringComparison.OrdinalIgnoreCase));
    }
}
