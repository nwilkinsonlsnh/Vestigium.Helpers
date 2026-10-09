using Vestigium.Helpers.Watch.Dns;
using Xunit;

namespace Vestigium.Helpers.Watch.Dns.Tests;

public sealed class ElevationTests
{
    [Fact]
    public void Unelevated_run_exits_2_before_a_sensor()
    {
        Assert.Equal(2, Program.NotElevated);
        if (Elevation.IsElevated())
            return;

        Assert.Equal(Program.NotElevated, Program.Main());
    }
}
