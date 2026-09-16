using System.Text;
using System.Xml;
using Vestigium.Helpers.Tests.Support;
using Vestigium.Helpers.Xml;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class XmlFileTests
{
    public XmlFileTests()
    {
        var area = Path.Combine(Path.GetTempPath(), "VestigiumXmlTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(area);
        Environment.SetEnvironmentVariable("VESTIGIUM_XML_TESTAREA", area);
        XmlTestHooks.ExportRoot = Path.Combine(area, "exports");
        XmlContentSeeder.ResetForTests();
        XmlContentSeeder.EnsureSeeded();
    }

    [Fact]
    public void Seeder_writes_all_six_gold_files()
    {
        foreach (var file in XmlContentSeeder.RequiredFiles)
        {
            var path = XmlContentSeeder.GetPath(file);
            Assert.True(File.Exists(path), file);
            Assert.True(new FileInfo(path).Length > 0, file);
        }
    }

    [Fact]
    public void Scratch_copy_does_not_write_gold()
    {
        var gold = Path.Combine(XmlContentSeeder.ContentDirectory, "osinfo.xml");
        var before = XmlContentSeeder.ComputeSha256(gold);
        var scratch = XmlContentSeeder.CreateScratchCopy("osinfo.xml");
        File.WriteAllText(scratch, "<root/>");
        Assert.Equal(before, XmlContentSeeder.ComputeSha256(gold));
    }

    [Fact]
    public void Open_diagwrn_throws_OpenMulti_yields_documents()
    {
        var path = XmlContentSeeder.GetPath("diagwrn.xml");
        Assert.Throws<XmlException>(() => XmlHelper.Open(path));
        using var stream = XmlHelper.OpenMulti(path);
        Assert.True(stream.Count > 1);
        var hits = 0;
        foreach (var part in stream)
            hits += part.Count(XmlSearch.ByAttribute("Fun"));
        Assert.True(hits > 0);
    }

    [Fact]
    public void Encoding_utf8_bom_and_declaration_conflict_bom_wins()
    {
        var dir = XmlTestHooks.ExportRoot!;
        Directory.CreateDirectory(dir);
        var bomPath = Path.Combine(dir, "bom.xml");
        var utf16Path = Path.Combine(dir, "utf16.xml");
        File.WriteAllBytes(bomPath, [0xEF, 0xBB, 0xBF, .. Encoding.UTF8.GetBytes("<?xml version=\"1.0\" encoding=\"utf-8\"?><root id=\"1\"/>")]);
        File.WriteAllBytes(utf16Path, Encoding.Unicode.GetPreamble().Concat(Encoding.Unicode.GetBytes("<?xml version=\"1.0\" encoding=\"utf-16\"?><root id=\"2\"/>")).ToArray());

        using (var utf8 = XmlHelper.Open(bomPath))
        {
            Assert.Equal("utf-8", utf8.EncodingName);
            Assert.Equal("bom", utf8.EncodingSource);
        }

        using var utf16 = XmlHelper.Open(utf16Path);
        Assert.Equal("utf-16le", utf16.EncodingName);
        Assert.Equal("bom", utf16.EncodingSource);
    }

    [Fact]
    public void Text_xml_and_application_xml_are_aliases()
    {
        Assert.True(XmlMediaType.AreAliases("text/xml", "application/xml"));
        Assert.True(XmlMediaType.IsXmlFamily("application/vnd.oma.ddf+xml"));
        Assert.Equal("application/xml; charset=utf-8", XmlMediaType.ApplicationXml.ToContentType());
    }

    [Fact]
    public void WriteFile_collision_default_fail()
    {
        var dest = Path.Combine(XmlTestHooks.ExportRoot!, "exists.xml");
        Directory.CreateDirectory(XmlTestHooks.ExportRoot!);
        XmlHelper.WriteFile(dest, XmlHelper.Parse("<a/>"));
        Assert.Throws<IOException>(() => XmlHelper.WriteFile(dest, XmlHelper.Parse("<b/>")));
        XmlHelper.WriteFile(dest, XmlHelper.Parse("<b/>"), new XmlWriteOptions { Collision = XmlCollision.Overwrite });
    }

    [Fact]
    public void PageEnabled_and_unenroll_are_findable()
    {
        using var prep = XmlHelper.Open(XmlContentSeeder.GetPath("DevicePreparationDDF.xml"));
        Assert.NotNull(prep.First(XmlSearch.ByName("NodeName").WhereText("PageEnabled")));

        using var dm = XmlHelper.Open(XmlContentSeeder.GetPath("DMClient_DDF.xml"));
        Assert.NotEmpty(dm.Search(XmlSearch.ByText("Unenroll")));
        Assert.NotEmpty(dm.Search(XmlSearch.ByName("Exec")));
    }
}
