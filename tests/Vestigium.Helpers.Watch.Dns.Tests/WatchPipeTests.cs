using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;
using Vestigium.Helpers.Watch.Dns;
using Xunit;

namespace Vestigium.Helpers.Watch.Dns.Tests;

public sealed class WatchPipeTests
{
    [Fact(Timeout = 3000)]
    public async Task A_row_is_one_json_line()
    {
        var name = "watch-dns-" + Guid.NewGuid().ToString("N");
        await using var pipe = WatchPipe.Create(name);
        using var client = new NamedPipeClientStream(".", name, PipeDirection.InOut, PipeOptions.Asynchronous);
        var connect = client.ConnectAsync(1_000);
        await pipe.WaitForClientAsync(new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token);
        await connect;

        using var reader = new StreamReader(client, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, leaveOpen: true);
        var reading = reader.ReadLineAsync();
        var row = new WatchRow(DateTimeOffset.Parse("2026-10-09T09:54:00Z"), "chrome", 44, "edge.example", "A", "0", "1.2.3.4", "resolver");
        await pipe.WriteAsync(row, CancellationToken.None);
        var line = await reading.WaitAsync(TimeSpan.FromSeconds(1));
        var back = JsonSerializer.Deserialize<WatchRow>(line!, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        Assert.NotNull(back);
        Assert.Equal("edge.example", back.Name);
        Assert.Equal(44, back.Pid);
        Assert.Equal("resolver", back.Mode);
        Assert.DoesNotContain("\n", line);
    }

    [Fact(Timeout = 3000)]
    public void Acl_is_the_current_user_and_administrators()
    {
        if (!OperatingSystem.IsWindows())
            return;

        var security = WatchPipe.BuildSecurity();
        var rules = security.GetAccessRules(true, false, typeof(SecurityIdentifier)).Cast<PipeAccessRule>().ToList();
        var user = WindowsIdentity.GetCurrent().User;
        Assert.Contains(rules, rule => rule.IdentityReference.Equals(user) && rule.AccessControlType == AccessControlType.Allow);
        Assert.Contains(rules, rule =>
            rule.IdentityReference.Equals(new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null))
            && rule.AccessControlType == AccessControlType.Allow);
    }
}
