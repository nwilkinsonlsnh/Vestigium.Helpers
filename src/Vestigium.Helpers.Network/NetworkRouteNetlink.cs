using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class NetworkRouteNetlink
{
    const int AfNetlink = 16;
    const int AfInet = 2;
    const int AfInet6 = 10;
    const int SockRaw = 3;
    const int SockCloexec = 0x80000;
    const int NetlinkRoute = 0;
    const ushort RtmNewRoute = 24;
    const ushort RtmDelRoute = 25;
    const ushort NlmsgError = 2;
    const ushort NlmFRequest = 0x01;
    const ushort NlmFAck = 0x04;
    const ushort NlmFReplace = 0x100;
    const ushort NlmFExcl = 0x200;
    const ushort NlmFCreate = 0x400;
    const byte RtnUnicast = 1;
    const byte RtProtStatic = 4;
    const byte RtScopeUniverse = 0;
    const byte RtTableMain = 254;
    const ushort RtaDst = 1;
    const ushort RtaOif = 4;
    const ushort RtaGateway = 5;
    const ushort RtaPriority = 6;
    const int Eperm = 1;
    const int Eacces = 13;
    const int Enetunreach = 101;

    public static void Add(NetworkRouteChange change) => Apply(change, create: true, replace: false);
    public static void Change(NetworkRouteChange change) => Apply(change, create: true, replace: true);
    public static void Remove(NetworkRouteChange change) => Apply(change, create: false, replace: false);

    static void Apply(NetworkRouteChange change, bool create, bool replace)
    {
        if (!OperatingSystem.IsLinux())
            throw NetworkRouteMutation.LinuxWriteDenied(nameof(Apply));

        var spec = NetworkRouteSpec.Parse(change);
        var ifIndex = spec.InterfaceIndex;
        if (ifIndex < 1)
            throw new ArgumentException("Linux route write requires InterfaceIndex.", nameof(change.InterfaceIndex));

        ushort flags = (ushort)(NlmFRequest | NlmFAck);
        ushort type = create ? RtmNewRoute : RtmDelRoute;
        if (create && replace)
            flags |= (ushort)(NlmFCreate | NlmFReplace);
        else if (create)
            flags |= (ushort)(NlmFCreate | NlmFExcl);

        var payload = Build(spec, type, flags, ifIndex);
        Send(payload, create ? nameof(Add) : nameof(Remove));
    }

    static byte[] Build(NetworkRouteSpec spec, ushort type, ushort flags, int ifIndex)
    {
        var family = (byte)(spec.IsIPv6 ? AfInet6 : AfInet);
        var body = new List<byte>(96);
        body.AddRange(new byte[16]);
        body.Add(family);
        body.Add((byte)spec.PrefixLength);
        body.Add(0);
        body.Add(0);
        body.Add(RtTableMain);
        body.Add(RtProtStatic);
        body.Add(RtScopeUniverse);
        body.Add(RtnUnicast);
        body.AddRange(BitConverter.GetBytes(0));
        AddAttr(body, RtaDst, spec.Destination.GetAddressBytes());
        AddAttr(body, RtaGateway, spec.Gateway.GetAddressBytes());
        AddAttr(body, RtaOif, BitConverter.GetBytes(ifIndex));
        AddAttr(body, RtaPriority, BitConverter.GetBytes(spec.Metric));

        var bytes = body.ToArray();
        BitConverter.GetBytes(bytes.Length).CopyTo(bytes, 0);
        BitConverter.GetBytes(type).CopyTo(bytes, 4);
        BitConverter.GetBytes(flags).CopyTo(bytes, 6);
        BitConverter.GetBytes(1).CopyTo(bytes, 8);
        return bytes;
    }

    static void AddAttr(List<byte> body, ushort type, byte[] value)
    {
        var len = (ushort)(4 + value.Length);
        body.AddRange(BitConverter.GetBytes(len));
        body.AddRange(BitConverter.GetBytes(type));
        body.AddRange(value);
        while (body.Count % 4 != 0)
            body.Add(0);
    }

    static void Send(byte[] payload, string verb)
    {
        var fd = socket(AfNetlink, SockRaw | SockCloexec, NetlinkRoute);
        if (fd < 0)
            throw Denied(verb, Marshal.GetLastPInvokeError());

        try
        {
            var addr = new SockaddrNl { nl_family = AfNetlink };
            if (bind(fd, ref addr, 12) < 0)
                throw Denied(verb, Marshal.GetLastPInvokeError());
            if (send(fd, payload, payload.Length, 0) < 0)
                throw Denied(verb, Marshal.GetLastPInvokeError());

            var reply = new byte[1024];
            var n = recv(fd, reply, reply.Length, 0);
            if (n < 16)
                throw Denied(verb, Marshal.GetLastPInvokeError());

            var msgType = BitConverter.ToUInt16(reply, 4);
            if (msgType != NlmsgError)
                return;
            var error = BitConverter.ToInt32(reply, 16);
            if (error != 0)
                throw Denied(verb, -error);
        }
        finally
        {
            _ = close(fd);
        }
    }

    internal static NetworkRouteDenied Denied(string verb, int errno)
    {
        var message = errno is Eperm or Eacces
            ? "Linux route write requires CAP_NET_ADMIN."
            : errno == Enetunreach
                ? verb + " rejected. Gateway or interface is unreachable."
                : verb + " failed. errno=" + errno;
        HelperLog.Reject(HelperLog.AppIds.Network, HelperLog.Subcategories.Route, verb, message);
        return new NetworkRouteDenied(message);
    }

    [StructLayout(LayoutKind.Sequential)]
    struct SockaddrNl
    {
        public ushort nl_family;
        public ushort nl_pad;
        public int nl_pid;
        public uint nl_groups;
    }

    [DllImport("libc", SetLastError = true)]
    static extern int socket(int domain, int type, int protocol);

    [DllImport("libc", SetLastError = true)]
    static extern int bind(int fd, ref SockaddrNl addr, uint addrlen);

    [DllImport("libc", SetLastError = true)]
    static extern int close(int fd);

    [DllImport("libc", SetLastError = true)]
    static extern int send(int fd, byte[] buf, int len, int flags);

    [DllImport("libc", SetLastError = true)]
    static extern int recv(int fd, byte[] buf, int len, int flags);
}
