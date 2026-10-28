using Vestigium.Helpers.Network;

namespace Vestigium.Helpers.Tests;

public sealed class NetworkPrintAsyncTests
{
    [Fact]
    public async Task Cancelled_token_throws_before_the_hop()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        await Assert.ThrowsAsync<OperationCanceledException>(() => NetworkHelper.GetRoutesAsync(RouteFamily.Pv4, cts.Token));
        Assert.Equal(
            typeof(Task<IReadOnlyList<NetworkRoute>>),
            typeof(NetworkHelper).GetMethod(nameof(NetworkHelper.GetRoutesAsync))!.ReturnType);
    }
}
