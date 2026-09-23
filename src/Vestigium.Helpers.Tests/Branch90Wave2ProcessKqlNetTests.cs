using Vestigium.Helpers.Kql;
using Vestigium.Helpers.Network;
using Vestigium.Helpers.Processes;

namespace Vestigium.Helpers.Tests;

[Collection("Logger")]
public sealed class Branch90Wave2ProcessKqlNetTests
{
    [Fact]
    public void Image_reader_pe_and_junk()
    {
        Assert.True(ProcessImageReader.ReadType(0, null) is ProcessImageType.X64 or ProcessImageType.X86);
        Assert.True(ProcessImageReader.ReadType(0, "  ") is ProcessImageType.X64 or ProcessImageType.X86);
        ProcessImageReader.ReadVersion(null, out var d, out var c, out var v);
        Assert.Null(d);
        ProcessImageReader.ReadVersion("C:\\no-such-vest.exe", out _, out _, out _);

        var notepad = Path.Combine(Environment.SystemDirectory, "notepad.exe");
        if (File.Exists(notepad))
        {
            var kind = ProcessImageReader.ReadType(0, notepad);
            Assert.True(kind is ProcessImageType.X86 or ProcessImageType.X64 or ProcessImageType.Arm64 or ProcessImageType.Unknown);
            ProcessImageReader.ReadVersion(notepad, out _, out _, out _);
        }

        var dir = Path.Combine(Path.GetTempPath(), "vest-pe-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        try
        {
            var tiny = Path.Combine(dir, "tiny.bin");
            File.WriteAllBytes(tiny, [1, 2, 3]);
            Assert.Equal(ProcessImageType.Unknown, ProcessImageReader.ReadType(0, tiny));

            var mz = Path.Combine(dir, "mz.bin");
            var buf = new byte[80];
            buf[0] = (byte)'M';
            buf[1] = (byte)'Z';
            BitConverter.GetBytes(70).CopyTo(buf, 60);
            File.WriteAllBytes(mz, buf);
            Assert.Equal(ProcessImageType.Unknown, ProcessImageReader.ReadType(0, mz));

            var text = Path.Combine(dir, "t.txt");
            File.WriteAllText(text, new string('x', 80));
            Assert.Equal(ProcessImageType.Unknown, ProcessImageReader.ReadType(0, text));
        }
        finally { try { Directory.Delete(dir, true); } catch { } }

        _ = ProcessHelper.GetThreads(Environment.ProcessId, includeStack: true);
        _ = ProcessHelper.GetThreads(Environment.ProcessId, includeStack: false);
        _ = ProcessHelper.SearchThreads(Environment.ProcessId, "THR.Tid GT 0");
        _ = ProcessHelper.Start(new ProcessStartRequest
        {
            FileName = Path.Combine(Environment.SystemDirectory, "cmd.exe"),
            Arguments = "/c exit 0",
            CreateNoWindow = true,
            RedirectStandardIo = true,
            Verb = "open",
            Environment = new Dictionary<string, string> { ["VEST_B90"] = "1" }
        });
    }

    [Fact]
    public void Kql_unknown_and_in_between()
    {
        using var session = KqlHelper.Create(KqlPack.Process);
        var row = new KqlRow(session).Set("PID", 10).Set("Name", "app.exe");
        var compiled = KqlHelper.Compile("CommandLine IN ('a', 'b')", session);
        Assert.True(compiled.Ok);
        Assert.Equal(KqlTriState.Unknown, compiled.Query!.Evaluate(row));

        compiled = KqlHelper.Compile("CommandLine NOT IN ('a')", session);
        Assert.Equal(KqlTriState.Unknown, compiled.Query!.Evaluate(row));

        compiled = KqlHelper.Compile("CommandLine BETWEEN 'a' AND 'z'", session);
        Assert.Equal(KqlTriState.Unknown, compiled.Query!.Evaluate(row));

        compiled = KqlHelper.Compile("PID == 10 AND CommandLine == 'x'", session);
        Assert.Equal(KqlTriState.Unknown, compiled.Query!.Evaluate(row));

        compiled = KqlHelper.Compile("PID == 10 OR CommandLine == 'x'", session);
        Assert.True(compiled.Query!.Matches(row));

        compiled = KqlHelper.Compile("PID == 1 AND Name == 'nope'", session);
        Assert.False(compiled.Query!.Matches(row));

        compiled = KqlHelper.Compile("Name IN ('APP.EXE', 'z')", session);
        Assert.True(compiled.Query!.Matches(row));

        compiled = KqlHelper.Compile("Name NOT IN ('APP.EXE')", session);
        Assert.False(compiled.Query!.Matches(row));
    }

    [Fact]
    public void Route_denied_and_bind_errors()
    {
        var access = NetworkRouteMutation.Denied("AddRoute", 5);
        Assert.Contains("Administrator", access.Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("invalid", NetworkRouteMutation.Denied("ChangeRoute", 87).Message, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("Win32", NetworkRouteMutation.Denied("RemoveRoute", 99).Message, StringComparison.Ordinal);

        var name = NetworkRouteMutation.PersistentName(new NetworkRouteChange
        {
            Destination = "192.0.2.0",
            PrefixLength = 24,
            Gateway = "192.0.2.1",
            Metric = 0
        });
        Assert.Contains("192.0.2.0", name);
        Assert.True(NetworkRouteMutation.FirstIpv4Index() >= 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => NetworkHelper.AddRoute(new NetworkRouteChange
        {
            Destination = "192.0.2.0",
            PrefixLength = -1,
            Gateway = "192.0.2.1"
        }));
        Assert.Throws<ArgumentException>(() => NetworkHelper.ChangeRoute(new NetworkRouteChange
        {
            Destination = "not-an-ip",
            PrefixLength = 24,
            Gateway = "192.0.2.1"
        }));
        Assert.Throws<ArgumentException>(() => NetworkHelper.RemoveRoute(new NetworkRouteChange
        {
            Destination = "192.0.2.0",
            PrefixLength = 24,
            Gateway = "ffff::1"
        }));
    }
}
