using System.Runtime.InteropServices;

namespace Vestigium.Helpers.Services;

internal static class ServiceMachine
{
    private static readonly AsyncLocal<string?> Current = new();

    public static string? Name => Normalize(Current.Value);

    public static bool IsLocal => Name is null;

    public static IDisposable Push(string? machine)
    {
        var prior = Current.Value;
        Current.Value = Normalize(machine);
        return new Pop(prior);
    }

    public static string? Normalize(string? machine)
    {
        if (string.IsNullOrWhiteSpace(machine))
            return null;
        var text = machine.Trim().TrimStart('\\');
        if (text is "." or "localhost" || text.Equals(Environment.MachineName, StringComparison.OrdinalIgnoreCase))
            return null;
        return text;
    }

    public static string ControllerName => Name ?? ".";

    public static bool TryConnect(string? machine, out string? reason)
    {
        var target = Normalize(machine);
        var scm = ServiceNative.OpenSCManager(target, null, ServiceNative.ScManagerConnect | ServiceNative.ScManagerEnumerate);
        if (scm == 0)
        {
            reason = "OpenSCManager " + Marshal.GetLastWin32Error();
            return false;
        }

        ServiceNative.CloseServiceHandle(scm);
        reason = null;
        return true;
    }

    private sealed class Pop : IDisposable
    {
        private readonly string? _prior;
        public Pop(string? prior) => _prior = prior;
        public void Dispose() => Current.Value = _prior;
    }
}
