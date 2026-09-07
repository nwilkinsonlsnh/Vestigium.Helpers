using Vestigium.Helpers.Analytics;
using Vestigium.Helpers.ClosedXml;
using Vestigium.Helpers.Encryption;
using Vestigium.Helpers.FileIo;
using Vestigium.Helpers.Json;
using Vestigium.Helpers.Network;
using Vestigium.Helpers.Processes;
using Vestigium.Helpers.Services;
using Vestigium.Helpers.WinReg;
using Vestigium.Helpers.Xml;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class SkeletonSmokeTests
{
    [Fact]
    public void Core_identity_is_stable()
        => Assert.Equal("Vestigium.Helpers", HelperGuard.Identity);

    [Fact]
    public void NotBlank_rejects_whitespace()
        => Assert.Throws<ArgumentException>(() => HelperGuard.NotBlank("  ", "name"));

    [Fact]
    public void Library_identities_match_project_names()
    {
        Assert.Equal("Vestigium.Helpers.ClosedXml", WorkbookHelper.Identity);
        Assert.Equal("Vestigium.Helpers.Encryption", EncryptionHelper.Identity);
        Assert.Equal("Vestigium.Helpers.WinReg", RegistryHelper.Identity);
        Assert.Equal("Vestigium.Helpers.Json", JsonHelper.Identity);
        Assert.Equal("Vestigium.Helpers.Xml", XmlHelper.Identity);
        Assert.Equal("Vestigium.Helpers.FileIo", FileIoHelper.Identity);
        Assert.Equal("Vestigium.Helpers.Processes", ProcessHelper.Identity);
        Assert.Equal("Vestigium.Helpers.Services", ServiceHelper.Identity);
        Assert.Equal("Vestigium.Helpers.Analytics", AnalyticsHelper.Identity);
        Assert.Equal("Vestigium.Helpers.Network", NetworkHelper.Identity);
    }
}
