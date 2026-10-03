using Vestigium.Helpers.Network;

var box = NetworkHelper.GetWorkstation();
var enabled = NetworkHelper.GetAdapters(new NetworkAdapterQuery { IncludeDown = true, IpEnabledOnly = true });
Console.WriteLine($"Helpers.Network inventory lab  {typeof(NetworkHelper).Assembly.GetName().Version}");
Console.WriteLine($"Host     {box.HostName}");
Console.WriteLine($"Domain   {box.DomainName ?? "—"}");
Console.WriteLine($"Captured {box.CapturedUtc:u}");
Console.WriteLine($"Search   {Join(box.DnsSuffixSearchList)}");
Console.WriteLine($"         (primary suffix + SearchList + each adapter DnsSuffix. Empty SearchList key is normal.)");
if (box.Stack is { } stack)
{
    Console.WriteLine("Stack");
    Console.WriteLine($"  Hostname     {Blank(stack.HostName)}");
    Console.WriteLine($"  Domain       {Blank(stack.Domain)}");
    Console.WriteLine($"  NV Domain    {Blank(stack.NvDomain)}");
    Console.WriteLine($"  DHCP domain  {Blank(stack.DhcpDomain)}");
    Console.WriteLine($"  NameServer   {Blank(stack.NameServer)}");
    Console.WriteLine($"  DHCP DNS     {Join(stack.DhcpNameServers)}");
    Console.WriteLine($"  Router       {Flag(stack.IpEnableRouter)}");
    Console.WriteLine($"  Sync domain  {Flag(stack.SyncDomainWithMembership)}");
}
Console.WriteLine($"Adapters {enabled.Count} IP-enabled  ({box.Adapters.Count} total, {box.Adapters.Count - enabled.Count} bind-off hidden)");
Console.WriteLine();

foreach (var nic in enabled)
{
    Console.WriteLine(new string('-', 72));
    Console.WriteLine($"{nic.Name}  [{nic.Status}]  {nic.Type}");
    Console.WriteLine($"  Id            {nic.Id}");
    Console.WriteLine($"  Description   {Blank(nic.Description)}");
    Console.WriteLine($"  MAC           {Blank(nic.MacAddress)}");
    Console.WriteLine($"  Speed         {Speed(nic.SpeedBitsPerSecond)}");
    Console.WriteLine($"  IfIndex       {Num(nic.InterfaceIndex)}");
    Console.WriteLine($"  IPv4 metric   {Metric(nic)}");
    Console.WriteLine($"  Autoconfig    {Flag(nic.Ipv4AutoconfigEnabled)}");
    Console.WriteLine($"  MTU           {Num(nic.Mtu)}");
    Console.WriteLine($"  DNS suffix    {Blank(nic.DnsSuffix)}");
    Console.WriteLine($"  Register DNS  {Flag(nic.DnsRegistrationEnabled)}");
    Console.WriteLine($"  DNS servers   {Join(nic.DnsServers)}");
    Console.WriteLine($"  WINS          {Join(nic.WinsServers)}");
    Console.WriteLine($"  Gateways      {Join(nic.Gateways)}");
    Console.WriteLine($"  DHCP          enabled={Flag(nic.Dhcp.IsEnabled)}  server={Blank(nic.Dhcp.Server)}");
    Console.WriteLine($"  Lease         obtained={Time(nic.Dhcp.LeaseObtained)}  expires={Time(nic.Dhcp.LeaseExpires)}");
    Console.WriteLine($"  NetBIOS       {nic.NetbiosOverTcp}");
    Console.WriteLine($"  Physical      {Flag(nic.PhysicalAdapter)}");
    Console.WriteLine($"  IP enabled    {YesNo(nic.IpEnabled)}  IPv4={YesNo(nic.SupportsIpv4)}/{YesNo(nic.HasIpv4Unicast)}  IPv6={YesNo(nic.SupportsIpv6)}/{YesNo(nic.HasIpv6Unicast)}");
    Console.WriteLine("  Unicast");
    if (nic.UnicastAddresses.Count == 0)
        Console.WriteLine("    —");
    else
    {
        foreach (var address in nic.UnicastAddresses)
            Console.WriteLine($"    {address.Address}/{address.PrefixLength}  {address.Family}  mask={Blank(address.SubnetMask)}  dhcp={address.IsDhcpAssigned}");
    }

    if (nic.Driver is null)
        Console.WriteLine("  Driver        —");
    else
    {
        Console.WriteLine($"  Driver        {Blank(nic.Driver.Description)}");
        Console.WriteLine($"    Provider    {Blank(nic.Driver.Provider)}");
        Console.WriteLine($"    Version     {Blank(nic.Driver.Version)}");
        Console.WriteLine($"    Date        {Time(nic.Driver.Date)}");
        Console.WriteLine($"    INF         {Blank(nic.Driver.Inf)}");
        Console.WriteLine($"    Hardware    {Blank(nic.Driver.HardwareId)}");
        Console.WriteLine($"    Service     {Blank(nic.Driver.Service)}");
    }

    Console.WriteLine();
}

var names = NetworkHelper.GetNetBiosNames();
var stats = NetworkHelper.GetNetBiosStats();
Console.WriteLine(new string('-', 72));
Console.WriteLine($"NetBIOS  node={stats.NodeType ?? "—"}  names={names.Count}");
foreach (var row in names)
    Console.WriteLine($"  {row.Name}<{row.Suffix}>  {row.SuffixName}  {row.Type}  {row.Status}  adapter={row.Adapter ?? "—"}  node={row.NodeAddress ?? "—"}");
if (names.Count == 0)
    Console.WriteLine("  No names. The node value is the Netbios code when the call failed.");
Console.WriteLine();
Console.WriteLine("Done. Compare a physical NIC to ipconfig /all and the driver tab.");
Console.WriteLine("Empty WINS, leases, or suffix on loopback is expected.");

static string Blank(string? value) => string.IsNullOrWhiteSpace(value) ? "—" : value.Trim();

static string Num(int? value) => value is null ? "—" : value.Value.ToString();

static string Flag(bool? value) => value is null ? "—" : value.Value ? "yes" : "no";

static string YesNo(bool value) => value ? "yes" : "no";

static string Time(DateTimeOffset? value) => value is null ? "—" : value.Value.ToLocalTime().ToString("g");

static string Join(IReadOnlyList<string> values)
    => values.Count == 0 ? "—" : string.Join(", ", values);

static string Speed(long? bits)
{
    if (bits is null)
        return "—";
    if (bits >= 1_000_000_000)
        return $"{bits.Value / 1_000_000_000d:0.###} Gbps";
    if (bits >= 1_000_000)
        return $"{bits.Value / 1_000_000d:0.###} Mbps";
    return $"{bits} bps";
}

static string Metric(NetworkAdapter nic)
{
    if (nic.Ipv4Metric is int metric)
        return nic.Ipv4MetricIsAutomatic == true ? $"{metric} auto" : metric.ToString();
    return nic.Ipv4MetricIsAutomatic == true ? "auto" : "—";
}
