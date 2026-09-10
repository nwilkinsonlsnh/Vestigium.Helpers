using System.Globalization;
using System.Net;
using System.Net.Sockets;
using System.Numerics;
using Vestigium.Helpers;

namespace Vestigium.Helpers.Network;

internal static class SubnetEngine
{
    public static AddressClass Classify(string address)
    {
        var ip = ParseIp(address);
        var klass = ClassOf(ip);
        var kind = KindOf(ip);
        return new AddressClass(ip.AddressFamily, Format(ip), klass, kind);
    }

    public static PrefixBlock Describe(string cidr)
    {
        var (ip, prefix) = ParseCidr(cidr);
        return Describe(ip, prefix, Format(ip));
    }

    public static PrefixBlock Describe(string address, int prefixLength)
    {
        var ip = ParseIp(address);
        EnsurePrefix(ip.AddressFamily, prefixLength);
        return Describe(ip, prefixLength, Format(ip));
    }

    public static PrefixBlock DescribeMask(string address, string dottedMask)
    {
        var ip = ParseIp(address);
        if (ip.AddressFamily != AddressFamily.InterNetwork)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, Subcat(), nameof(DescribeMask), "dotted mask requires IPv4");
            throw new ArgumentException("Dotted subnet masks are IPv4 only.", nameof(dottedMask));
        }

        if (!IPAddress.TryParse(dottedMask, out var mask) || mask.AddressFamily != AddressFamily.InterNetwork)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, Subcat(), nameof(DescribeMask), "mask unparsable");
            throw new ArgumentException("Subnet mask is not a valid IPv4 address.", nameof(dottedMask));
        }

        var prefix = Ipv4Prefix.PrefixFromMask(mask);
        return Describe(ip, prefix, Format(ip));
    }

    public static PrefixPlan PlanByHosts(string parentCidr, int minimumHosts, SubnetQuery? query)
    {
        if (minimumHosts < 1)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, Subcat(), nameof(PlanByHosts), $"hosts={minimumHosts}");
            throw new ArgumentOutOfRangeException(nameof(minimumHosts), "Minimum hosts must be at least 1.");
        }

        var parent = Describe(parentCidr);
        var q = Query(query);
        var child = PrefixForHosts(parent, minimumHosts, q.CountNetworkAndBroadcast);
        return Split(parent, child, q, $"hosts>={minimumHosts}");
    }

    public static PrefixPlan PlanByNetworks(string parentCidr, int minimumNetworks, SubnetQuery? query)
    {
        if (minimumNetworks < 1)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, Subcat(), nameof(PlanByNetworks), $"networks={minimumNetworks}");
            throw new ArgumentOutOfRangeException(nameof(minimumNetworks), "Minimum networks must be at least 1.");
        }

        var parent = Describe(parentCidr);
        var q = Query(query);
        var extra = BitsForCount(minimumNetworks);
        var child = parent.PrefixLength + extra;
        var max = Width(parent.Family);
        if (child > max)
            TooSmall(parent, $"networks>={minimumNetworks}");
        return Split(parent, child, q, $"networks>={minimumNetworks}");
    }

    public static PrefixPlan SplitPrefix(string parentCidr, int childPrefix, SubnetQuery? query)
    {
        var parent = Describe(parentCidr);
        var q = Query(query);
        if (childPrefix < parent.PrefixLength || childPrefix > Width(parent.Family))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, Subcat(), nameof(SplitPrefix), $"child={childPrefix} parent={parent.PrefixLength}");
            throw new ArgumentOutOfRangeException(nameof(childPrefix), "Child prefix must be between the parent prefix and the address width.");
        }

        return Split(parent, childPrefix, q, $"split=/{childPrefix}");
    }

    public static PrefixPlan SplitByCount(string parentCidr, int count, SubnetQuery? query)
        => PlanByNetworks(parentCidr, count, query);

    public static PrefixPlan PackVlsm(string parentCidr, IReadOnlyList<int> hostNeeds, SubnetQuery? query)
    {
        if (hostNeeds is null || hostNeeds.Count == 0)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, Subcat(), nameof(PackVlsm), "empty VLSM list");
            throw new ArgumentException("VLSM needs at least one host count.", nameof(hostNeeds));
        }

        var parent = Describe(parentCidr);
        var q = Query(query);
        var needs = hostNeeds.ToList();
        if (q.PackLargestFirst)
            needs.Sort((a, b) => b.CompareTo(a));

        var width = Width(parent.Family);
        var parentStart = ToInt(ParseIp(parent.Network));
        var parentSize = BigInteger.One << (width - parent.PrefixLength);
        var parentEnd = parentStart + parentSize;
        var cursor = parentStart;
        var packed = new List<PrefixBlock>();

        foreach (var need in needs)
        {
            if (need < 1)
            {
                HelperLog.Reject(HelperLog.AppIds.Network, Subcat(), nameof(PackVlsm), $"need={need}");
                throw new ArgumentOutOfRangeException(nameof(hostNeeds), "Each VLSM need must be at least 1.");
            }

            var childPrefix = PrefixForHosts(parent, need, q.CountNetworkAndBroadcast);
            var blockSize = BigInteger.One << (width - childPrefix);
            var aligned = AlignUp(cursor, blockSize);
            if (aligned + blockSize > parentEnd)
                TooSmall(parent, $"vlsm need={need}");
            packed.Add(FromNetwork(parent.Family, aligned, childPrefix, null));
            cursor = aligned + blockSize;
        }

        var unused = GreedyRemainders(parent.Family, cursor, parentEnd, q.MaxList);
        NetworkLog.Success(Subcat(), $"vlsm parent={parent.Network}/{parent.PrefixLength} packed={packed.Count} unused={unused.Count}");
        return new PrefixPlan(parent, "vlsm", packed[0].PrefixLength, packed.Count, packed, unused);
    }

    public static bool Contains(string prefixCidr, string address)
    {
        var block = Describe(prefixCidr);
        var ip = ParseIp(address);
        if (ip.AddressFamily != block.Family)
            return false;
        var start = ToInt(ParseIp(block.Network));
        var value = ToInt(ip);
        var size = BigInteger.One << (Width(block.Family) - block.PrefixLength);
        return value >= start && value < start + size;
    }

    public static bool Overlaps(string leftCidr, string rightCidr)
    {
        var left = Describe(leftCidr);
        var right = Describe(rightCidr);
        if (left.Family != right.Family)
            return false;
        var a0 = ToInt(ParseIp(left.Network));
        var b0 = ToInt(ParseIp(right.Network));
        var a1 = a0 + (BigInteger.One << (Width(left.Family) - left.PrefixLength));
        var b1 = b0 + (BigInteger.One << (Width(right.Family) - right.PrefixLength));
        return a0 < b1 && b0 < a1;
    }

    public static PrefixBlock Summarize(IEnumerable<string> cidrs)
    {
        var blocks = cidrs.Select(Describe).ToList();
        if (blocks.Count == 0)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, Subcat(), nameof(Summarize), "empty set");
            throw new ArgumentException("Summarize needs at least one prefix.");
        }

        var family = blocks[0].Family;
        if (blocks.Exists(b => b.Family != family))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, Subcat(), nameof(Summarize), "mixed families");
            throw new ArgumentException("Summarize cannot mix IPv4 and IPv6.");
        }

        var width = Width(family);
        var min = ToInt(ParseIp(blocks[0].Network));
        var maxExclusive = min + (BigInteger.One << (width - blocks[0].PrefixLength));
        foreach (var block in blocks.Skip(1))
        {
            var start = ToInt(ParseIp(block.Network));
            var end = start + (BigInteger.One << (width - block.PrefixLength));
            if (start < min) min = start;
            if (end > maxExclusive) maxExclusive = end;
        }

        var span = maxExclusive - min;
        var prefix = width;
        while (prefix > 0)
        {
            var size = BigInteger.One << (width - prefix + 1);
            if ((min & (size - 1)) != 0 || size < span)
                break;
            prefix--;
        }

        while (true)
        {
            var mask = PrefixMask(width, prefix);
            var aligned = min & mask;
            var size = BigInteger.One << (width - prefix);
            if (aligned == min && aligned + size >= maxExclusive)
                break;
            if (prefix == 0)
                break;
            prefix--;
            min = aligned & PrefixMask(width, prefix);
        }

        min &= PrefixMask(width, prefix);
        return FromNetwork(family, min, prefix, null);
    }

    public static PrefixBlock? NextBlock(string cidr)
    {
        var block = Describe(cidr);
        var width = Width(block.Family);
        var start = ToInt(ParseIp(block.Network));
        var size = BigInteger.One << (width - block.PrefixLength);
        var next = start + size;
        var max = BigInteger.One << width;
        if (next >= max)
            return null;
        return FromNetwork(block.Family, next, block.PrefixLength, null);
    }

    static PrefixPlan Split(PrefixBlock parent, int childPrefix, SubnetQuery query, string rule)
    {
        var width = Width(parent.Family);
        var extra = childPrefix - parent.PrefixLength;
        var total = BigInteger.One << extra;
        var start = ToInt(ParseIp(parent.Network));
        var step = BigInteger.One << (width - childPrefix);
        var take = (int)BigInteger.Min(total, query.MaxList);
        var rows = new List<PrefixBlock>(take);
        for (var i = 0; i < take; i++)
            rows.Add(FromNetwork(parent.Family, start + step * i, childPrefix, null));
        NetworkLog.Success(Subcat(), $"{rule} parent={parent.Network}/{parent.PrefixLength} child=/{childPrefix} total={total} listed={rows.Count}");
        return new PrefixPlan(parent, rule, childPrefix, total, rows, []);
    }

    static PrefixBlock Describe(IPAddress ip, int prefix, string? original)
    {
        EnsurePrefix(ip.AddressFamily, prefix);
        var value = ToInt(ip);
        var network = value & PrefixMask(Width(ip.AddressFamily), prefix);
        return FromNetwork(ip.AddressFamily, network, prefix, original);
    }

    static PrefixBlock FromNetwork(AddressFamily family, BigInteger network, int prefix, string? original)
    {
        var width = Width(family);
        var size = BigInteger.One << (width - prefix);
        var last = network + size - 1;
        var networkIp = ToIp(family, network);
        var lastIp = ToIp(family, last);
        string? mask = null;
        string? wildcard = null;
        string? broadcast = null;
        string? firstUsable;
        string? lastUsable;
        BigInteger usable;
        var hostRoute = prefix == width;
        var p2p = family == AddressFamily.InterNetwork && prefix == 31
            || family == AddressFamily.InterNetworkV6 && prefix == 127;

        if (family == AddressFamily.InterNetwork)
        {
            mask = Ipv4Prefix.MaskFromPrefix(prefix);
            wildcard = Ipv4Prefix.MaskFromPrefix(32 - prefix == 32 ? 0 : 32 - prefix);
            if (prefix == 0)
                wildcard = "255.255.255.255";
            else
            {
                var wild = ~ToUint(IPAddress.Parse(mask));
                wildcard = new IPAddress(ToBytes4(wild)).ToString();
            }

            broadcast = lastIp.ToString();
            if (prefix <= 30)
            {
                firstUsable = ToIp(family, network + 1).ToString();
                lastUsable = ToIp(family, last - 1).ToString();
                usable = size - 2;
            }
            else if (prefix == 31)
            {
                firstUsable = networkIp.ToString();
                lastUsable = lastIp.ToString();
                usable = 2;
            }
            else
            {
                firstUsable = networkIp.ToString();
                lastUsable = networkIp.ToString();
                usable = 1;
            }
        }
        else
        {
            firstUsable = networkIp.ToString();
            lastUsable = lastIp.ToString();
            usable = size;
        }

        var klass = ClassOf(networkIp);
        var kind = KindOf(networkIp);
        string? ptr = null;
        if (family == AddressFamily.InterNetwork && prefix == 24)
        {
            var b = networkIp.GetAddressBytes();
            ptr = $"{b[2]}.{b[1]}.{b[0]}.in-addr.arpa";
        }

        return new PrefixBlock(
            family,
            original,
            networkIp.ToString(),
            prefix,
            mask,
            wildcard,
            broadcast,
            firstUsable,
            lastUsable,
            size,
            usable,
            hostRoute,
            p2p,
            BinaryMask(family, prefix),
            klass,
            kind,
            ptr);
    }

    static int PrefixForHosts(PrefixBlock parent, int hosts, bool countNetworkAndBroadcast)
    {
        var width = Width(parent.Family);
        for (var prefix = width; prefix >= parent.PrefixLength; prefix--)
        {
            if (Usable(parent.Family, prefix, countNetworkAndBroadcast) >= hosts)
                return prefix;
        }

        TooSmall(parent, $"hosts>={hosts}");
        return parent.PrefixLength;
    }

    static BigInteger Usable(AddressFamily family, int prefix, bool countAll)
    {
        var width = Width(family);
        var size = BigInteger.One << (width - prefix);
        if (family == AddressFamily.InterNetworkV6 || countAll)
            return size;
        if (prefix >= 31)
            return size;
        return size - 2;
    }

    static List<PrefixBlock> GreedyRemainders(AddressFamily family, BigInteger start, BigInteger end, int maxList)
    {
        var rows = new List<PrefixBlock>();
        var width = Width(family);
        var cursor = start;
        while (cursor < end && rows.Count < maxList)
        {
            var remain = end - cursor;
            var bits = 0;
            var size = BigInteger.One;
            while (true)
            {
                var next = size << 1;
                if (next > remain)
                    break;
                if ((cursor & (next - 1)) != 0)
                    break;
                size = next;
                bits++;
                if (bits >= width)
                    break;
            }

            var prefix = width - bits;
            rows.Add(FromNetwork(family, cursor, prefix, null));
            cursor += size;
        }

        return rows;
    }

    static SubnetQuery Query(SubnetQuery? query)
    {
        var q = query ?? new SubnetQuery();
        if (q.MaxList < 1)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, Subcat(), nameof(Query), $"MaxList={q.MaxList}");
            throw new ArgumentOutOfRangeException(nameof(query), "MaxList must be at least 1.");
        }

        return q;
    }

    static (IPAddress ip, int prefix) ParseCidr(string cidr)
    {
        var text = HelperGuard.NotBlank(cidr, nameof(cidr)).Trim();
        var slash = text.LastIndexOf('/');
        if (slash < 0)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, Subcat(), nameof(ParseCidr), "missing prefix");
            throw new ArgumentException("CIDR must look like address/prefix.", nameof(cidr));
        }

        var ip = ParseIp(text[..slash]);
        if (!int.TryParse(text[(slash + 1)..], NumberStyles.Integer, CultureInfo.InvariantCulture, out var prefix))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, Subcat(), nameof(ParseCidr), "prefix unparsable");
            throw new ArgumentException("CIDR prefix is not an integer.", nameof(cidr));
        }

        EnsurePrefix(ip.AddressFamily, prefix);
        return (ip, prefix);
    }

    static IPAddress ParseIp(string address)
    {
        var text = HelperGuard.NotBlank(address, nameof(address)).Trim();
        if (!IPAddress.TryParse(text, out var ip))
        {
            HelperLog.Reject(HelperLog.AppIds.Network, Subcat(), nameof(ParseIp), "unparsable");
            throw new ArgumentException("Address is not a valid IP.", nameof(address));
        }

        return ip;
    }

    static void EnsurePrefix(AddressFamily family, int prefix)
    {
        var max = Width(family);
        if (prefix is < 0 || prefix > max)
        {
            HelperLog.Reject(HelperLog.AppIds.Network, Subcat(), nameof(EnsurePrefix), $"prefix={prefix}");
            throw new ArgumentOutOfRangeException(nameof(prefix), $"Prefix must be 0–{max}.");
        }
    }

    static void TooSmall(PrefixBlock parent, string rule)
    {
        HelperLog.Reject(HelperLog.AppIds.Network, Subcat(), nameof(TooSmall), $"{rule} parent={parent.Network}/{parent.PrefixLength}");
        throw new InvalidOperationException($"Parent {parent.Network}/{parent.PrefixLength} cannot satisfy {rule}.");
    }

    static TraditionalClass ClassOf(IPAddress ip)
    {
        if (ip.AddressFamily != AddressFamily.InterNetwork)
            return TraditionalClass.None;
        var o = ip.GetAddressBytes()[0];
        if (o <= 127) return TraditionalClass.A;
        if (o <= 191) return TraditionalClass.B;
        if (o <= 223) return TraditionalClass.C;
        if (o <= 239) return TraditionalClass.D;
        return TraditionalClass.E;
    }

    static AddressKind KindOf(IPAddress ip)
    {
        if (ip.AddressFamily == AddressFamily.InterNetworkV6)
            return Kind6(ip);
        var v = ToUint(ip);
        var kind = AddressKind.None;
        if (v == 0) kind |= AddressKind.Unspecified;
        if (v == 0xFFFFFFFFu) kind |= AddressKind.Broadcast;
        if ((v & 0xFF000000) == 0x7F000000) kind |= AddressKind.Loopback;
        if ((v & 0xF0000000) == 0xE0000000) kind |= AddressKind.Multicast;
        if ((v & 0xFFFF0000) == 0xA9FE0000) kind |= AddressKind.LinkLocal;
        if ((v & 0xFFC00000) == 0x64400000) kind |= AddressKind.Cgnat;
        if ((v & 0xFF000000) == 0x0A000000) kind |= AddressKind.Rfc1918;
        if ((v & 0xFFF00000) == 0xAC100000) kind |= AddressKind.Rfc1918;
        if ((v & 0xFFFF0000) == 0xC0A80000) kind |= AddressKind.Rfc1918;
        if ((v & 0xFFFFFF00) == 0xC0000200) kind |= AddressKind.Documentation;
        if ((v & 0xFFFFFF00) == 0xC6336400) kind |= AddressKind.Documentation;
        if ((v & 0xFFFFFF00) == 0xCB007100) kind |= AddressKind.Documentation;
        if ((v & 0xFFFFFF00) == 0xC0000000) kind |= AddressKind.Documentation;
        if ((v & 0xFFFE0000) == 0xC6120000) kind |= AddressKind.Benchmark;
        if ((kind & (AddressKind.Multicast | AddressKind.Broadcast | AddressKind.Unspecified)) == 0)
            kind |= AddressKind.Unicast;
        return kind;
    }

    static AddressKind Kind6(IPAddress ip)
    {
        var kind = AddressKind.None;
        if (ip.IsIPv4MappedToIPv6) kind |= AddressKind.Ipv4Mapped;
        var b = ip.GetAddressBytes();
        if (b.All(x => x == 0)) kind |= AddressKind.Unspecified;
        if (ip.Equals(IPAddress.IPv6Loopback)) kind |= AddressKind.Loopback;
        if (b[0] == 0xFF) kind |= AddressKind.Multicast;
        if (b[0] == 0xFE && (b[1] & 0xC0) == 0x80) kind |= AddressKind.LinkLocal;
        if ((b[0] & 0xFE) == 0xFC) kind |= AddressKind.UniqueLocal;
        if (b[0] == 0x20 && b[1] == 0x01 && b[2] == 0x0D && b[3] == 0xB8) kind |= AddressKind.Documentation;
        if (kind == AddressKind.None || kind == AddressKind.Ipv4Mapped)
            kind |= AddressKind.GlobalUnicast;
        return kind;
    }

    static int Width(AddressFamily family) => family == AddressFamily.InterNetwork ? 32 : 128;

    static int BitsForCount(int count)
    {
        var bits = 0;
        var slots = 1;
        while (slots < count)
        {
            bits++;
            slots <<= 1;
        }
        return bits;
    }

    static BigInteger AlignUp(BigInteger value, BigInteger size)
    {
        var rem = value % size;
        return rem == 0 ? value : value + (size - rem);
    }

    static BigInteger PrefixMask(int width, int prefix)
    {
        if (prefix <= 0) return BigInteger.Zero;
        if (prefix >= width) return (BigInteger.One << width) - 1;
        return ((BigInteger.One << prefix) - 1) << (width - prefix);
    }

    static BigInteger ToInt(IPAddress ip)
    {
        var bytes = ip.GetAddressBytes();
        return new BigInteger(bytes, isUnsigned: true, isBigEndian: true);
    }

    static uint ToUint(IPAddress ip)
    {
        var b = ip.GetAddressBytes();
        return ((uint)b[0] << 24) | ((uint)b[1] << 16) | ((uint)b[2] << 8) | b[3];
    }

    static byte[] ToBytes4(uint value)
        => [(byte)(value >> 24), (byte)(value >> 16), (byte)(value >> 8), (byte)value];

    static IPAddress ToIp(AddressFamily family, BigInteger value)
    {
        var width = Width(family);
        var raw = value.ToByteArray(isUnsigned: true, isBigEndian: true);
        var bytes = new byte[width / 8];
        var copy = Math.Min(raw.Length, bytes.Length);
        Buffer.BlockCopy(raw, 0, bytes, bytes.Length - copy, copy);
        return new IPAddress(bytes);
    }

    static string Format(IPAddress ip) => ip.ToString();

    static string BinaryMask(AddressFamily family, int prefix)
    {
        var width = Width(family);
        var ones = new string('1', prefix);
        var zeros = new string('0', width - prefix);
        var bits = ones + zeros;
        if (family == AddressFamily.InterNetwork)
            return string.Join('.', Enumerable.Range(0, 4).Select(i => bits.Substring(i * 8, 8)));
        return string.Join(':', Enumerable.Range(0, 8).Select(i => bits.Substring(i * 16, 16)));
    }

    static string Subcat() => "Subnet";
}
