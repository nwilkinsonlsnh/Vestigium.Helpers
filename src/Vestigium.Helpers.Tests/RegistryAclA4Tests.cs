using Vestigium.Helpers.WinReg;

namespace Vestigium.Helpers.Tests;

public sealed class RegistryAclA4Tests
{
    [Fact]
    public void Unknown_and_link_hash_raw_bytes_not_hex_text()
    {
        var bytes = new byte[] { 0xDE, 0xAD };
        var hexText = Convert.ToHexString(bytes);
        var raw = new RegistryValueInfo { Name = "x", Type = RegistryValueKind.Unknown, Data = bytes, DataText = hexText };
        var asText = new RegistryValueInfo { Name = "x", Type = RegistryValueKind.String, Data = hexText, DataText = hexText };
        Assert.NotEqual(RegistryHelper.HashValue(raw), RegistryHelper.HashValue(asText));

        var link = new RegistryValueInfo { Name = "x", Type = RegistryValueKind.Link, Data = bytes, DataText = hexText };
        var resource = new RegistryValueInfo { Name = "x", Type = RegistryValueKind.ResourceList, Data = bytes, DataText = hexText };
        Assert.Equal(64, RegistryHelper.HashValue(link).Length);
        Assert.NotEqual(RegistryHelper.HashValue(link), RegistryHelper.HashValue(resource));
    }
}
