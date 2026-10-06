using System.Text;
using Vestigium.Helpers.LogParser;
using Vestigium.Helpers.LogParser.Url;

namespace Vestigium.Helpers.LogParser.Url.Tests;

public sealed class Url01SkeletonTests
{
    [Fact]
    public void URL01_001_text_returns_no_hosts_yet()
    {
        var result = UrlReader.Read(new MemoryStream(Encoding.UTF8.GetBytes("https://q2prod.idbs-cloud.com:8443/")));
        Assert.Equal(LogFormat.Url, result.Format);
        Assert.Empty(result.Hosts);
    }

    [Fact]
    public void URL01_002_oversize_is_rejected()
    {
        var ex = Assert.Throws<InvalidDataException>(() => UrlReader.Read(new OversizeStream()));
        Assert.Equal("Text file is over 64 MB.", ex.Message);
    }

    private sealed class OversizeStream : MemoryStream
    {
        public override bool CanSeek => true;
        public override long Length => UrlReader.MaxBytes + 1;
    }
}
