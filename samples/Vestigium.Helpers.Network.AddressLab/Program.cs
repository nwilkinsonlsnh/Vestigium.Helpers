using System.Net.NetworkInformation;
using System.Net.Sockets;
using Microsoft.Win32;
using Vestigium.Helpers.Network;

Console.WriteLine("Address lab. Raw adapter, API addresses, registry key.");
Console.WriteLine();

var nics = NetworkInterface.GetAllNetworkInterfaces();
Console.WriteLine($"NetworkInterface count {nics.Length}");
Console.WriteLine();

foreach (var nic in nics)
{
    Console.WriteLine(new string('-', 72));
    Console.WriteLine($"{nic.Name}");
    Console.WriteLine($"  Id     {nic.Id}");
    Console.WriteLine($"  Desc   {nic.Description}");
    Console.WriteLine($"  Status {nic.OperationalStatus}  Type {nic.NetworkInterfaceType}");

    try
    {
        var props = nic.GetIPProperties();
        var addrs = props.UnicastAddresses.ToArray();
        Console.WriteLine($"  API unicast {addrs.Length}");
        foreach (var addr in addrs)
            Console.WriteLine($"    {addr.Address.AddressFamily}  {addr.Address}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  API failed  {ex.GetType().Name}  {ex.Message}");
    }

    DumpKey(@"SYSTEM\CurrentControlSet\Services\Tcpip\Parameters\Interfaces\" + nic.Id, "v4");
    DumpKey(@"SYSTEM\CurrentControlSet\Services\Tcpip6\Parameters\Interfaces\" + nic.Id, "v6");
}

Console.WriteLine();
Console.WriteLine(new string('=', 72));
Console.WriteLine("Helper GetAdapters");
foreach (var adapter in NetworkHelper.GetAdapters(new NetworkAdapterQuery { IncludeDown = true }))
{
    var ips = string.Join(", ", adapter.UnicastAddresses.Select(a => a.Address));
    Console.WriteLine($"  {adapter.Name}  id={adapter.Id}");
    Console.WriteLine($"    addresses [{ips}]");
}

static void DumpKey(string path, string label)
{
    try
    {
        using var key = Registry.LocalMachine.OpenSubKey(path);
        if (key is null)
        {
            Console.WriteLine($"  registry {label}  no key");
            return;
        }

        Console.WriteLine($"  registry {label}  {path}");
        foreach (var name in new[] { "DhcpIPAddress", "IPAddress", "DhcpSubnetMask", "SubnetMask" })
        {
            var value = key.GetValue(name);
            Console.WriteLine($"    {name}  {Format(value)}");
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"  registry {label} failed  {ex.GetType().Name}  {ex.Message}");
    }
}

static string Format(object? value) => value switch
{
    null => "(null)",
    string s => s,
    string[] many => "[" + string.Join(", ", many) + "]",
    _ => value.ToString() ?? ""
};
