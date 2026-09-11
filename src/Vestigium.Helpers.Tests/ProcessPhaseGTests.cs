using System.Security;
using Vestigium.Helpers;
using Vestigium.Helpers.Processes;
using Vestigium.Logging;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class ProcessPhaseGTests
{
    [Fact]
    public void Wow64_and_system32_share_comment_key()
    {
        var wow = ProcessCommentStore.Key(@"C:\Windows\SysWOW64\app.exe", "app.exe");
        var sys = ProcessCommentStore.Key(@"C:\Windows\System32\app.exe", "app.exe");
        Assert.Equal(sys, wow);
        Assert.Contains("System32", wow, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("SysWOW64", wow, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void StartAs_failure_log_has_no_password()
    {
        HelperLog.Shutdown();
        var dir = Path.Combine(Path.GetTempPath(), "VestigiumHelpersTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        HelperLog.InitializeHost(HelperLog.AppIds.Processes, cfg => cfg.LogDirectory = dir);
        try
        {
            using var secret = new SecureString();
            foreach (var ch in "SuperSecretPass!")
                secret.AppendChar(ch);
            secret.MakeReadOnly();
            var result = ProcessHelper.StartAs(
                new ProcessStartRequest { FileName = "cmd.exe", Arguments = "/c echo hi" },
                new ProcessStartAs { UserName = "nobody", Domain = ".", Password = secret, LoadUserProfile = false });
            Assert.False(result.Ok);
            Assert.Contains(
                HelperLog.RecentJsonLines,
                line => line.Contains("StartAs", StringComparison.Ordinal) && line.Contains("user=nobody", StringComparison.Ordinal));
            Assert.DoesNotContain(
                HelperLog.RecentJsonLines,
                line => line.Contains("SuperSecretPass", StringComparison.OrdinalIgnoreCase));
        }
        finally
        {
            HelperLog.Shutdown();
        }
    }
}
