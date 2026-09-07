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

    [Theory]
    [InlineData("Vestigium.Helpers.ClosedXml", Vestigium.Helpers.ClosedXml.WorkbookHelper.Identity)]
    [InlineData("Vestigium.Helpers.Encryption", Vestigium.Helpers.Encryption.EncryptionHelper.Identity)]
    [InlineData("Vestigium.Helpers.WinReg", Vestigium.Helpers.WinReg.RegistryHelper.Identity)]
    [InlineData("Vestigium.Helpers.Json", Vestigium.Helpers.Json.JsonHelper.Identity)]
    [InlineData("Vestigium.Helpers.Xml", Vestigium.Helpers.Xml.XmlHelper.Identity)]
    [InlineData("Vestigium.Helpers.FileIo", Vestigium.Helpers.FileIo.FileIoHelper.Identity)]
    [InlineData("Vestigium.Helpers.Processes", Vestigium.Helpers.Processes.ProcessHelper.Identity)]
    [InlineData("Vestigium.Helpers.Services", Vestigium.Helpers.Services.ServiceHelper.Identity)]
    [InlineData("Vestigium.Helpers.Analytics", Vestigium.Helpers.Analytics.AnalyticsHelper.Identity)]
    [InlineData("Vestigium.Helpers.Network", Vestigium.Helpers.Network.NetworkHelper.Identity)]
    public void Library_identity_matches_project_name(string expected, string actual)
        => Assert.Equal(expected, actual);
}
