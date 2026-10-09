using System.IO.Pipes;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Text;
using System.Text.Json;

namespace Vestigium.Helpers.Watch.Dns;

public sealed record WatchRow(
    DateTimeOffset Time,
    string Process,
    int Pid,
    string Name,
    string Type,
    string Status,
    string Answers,
    string Mode)
{
    public int ResolverCount { get; init; }
    public int PacketCount { get; init; }
    public int Total { get; init; }
}

public sealed class WatchPipe : IAsyncDisposable
{
    private static readonly JsonSerializerOptions Json = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private readonly NamedPipeServerStream _server;
    private StreamWriter? _writer;

    private WatchPipe(string name, NamedPipeServerStream server)
    {
        Name = name;
        _server = server;
    }

    private StreamWriter Writer
    {
        get
        {
            if (_writer is not null)
                return _writer;

            _writer = new StreamWriter(_server, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false), leaveOpen: true)
            {
                AutoFlush = true
            };
            return _writer;
        }
    }

    public string Name { get; }

    public static WatchPipe Create(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Pipe name is required.", nameof(name));

        var server = OperatingSystem.IsWindows()
            ? NamedPipeServerStreamAcl.Create(
                name,
                PipeDirection.InOut,
                maxNumberOfServerInstances: 1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous,
                inBufferSize: 0,
                outBufferSize: 0,
                BuildSecurity())
            : new NamedPipeServerStream(
                name,
                PipeDirection.InOut,
                maxNumberOfServerInstances: 1,
                PipeTransmissionMode.Byte,
                PipeOptions.Asynchronous);

        return new WatchPipe(name, server);
    }

    public static PipeSecurity BuildSecurity()
    {
        var security = new PipeSecurity();
        var user = WindowsIdentity.GetCurrent().User
            ?? throw new InvalidOperationException("The current user has no sid.");
        security.AddAccessRule(new PipeAccessRule(user, PipeAccessRights.FullControl, AccessControlType.Allow));
        security.AddAccessRule(new PipeAccessRule(
            new SecurityIdentifier(WellKnownSidType.BuiltinAdministratorsSid, null),
            PipeAccessRights.FullControl,
            AccessControlType.Allow));
        return security;
    }

    public Task WaitForClientAsync(CancellationToken token)
        => _server.WaitForConnectionAsync(token);

    public Task WriteAsync(WatchRow row, CancellationToken token)
        => Writer.WriteLineAsync(JsonSerializer.Serialize(row, Json).AsMemory(), token);

    public async ValueTask DisposeAsync()
    {
        if (_writer is not null) await _writer.DisposeAsync().ConfigureAwait(false);
        await _server.DisposeAsync().ConfigureAwait(false);
    }
}
