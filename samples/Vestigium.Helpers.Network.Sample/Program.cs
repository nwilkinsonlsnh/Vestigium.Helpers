using Vestigium.Helpers.Network;

var names = NetworkHelper.GetNetBiosNames();
var stats = NetworkHelper.GetNetBiosStats();
Console.WriteLine($"node={stats.NodeType ?? "--"} broadcast={stats.ResolvedByBroadcast} wins={stats.ResolvedByNameServer} registeredBroadcast={stats.RegisteredByBroadcast} registeredWins={stats.RegisteredByNameServer}");
Console.WriteLine($"names={names.Count}");
foreach (var row in names)
    Console.WriteLine($"{row.IsCache}\t{row.Adapter}\t{row.NodeAddress}\t{row.Name}\t{row.Suffix}\t{row.SuffixName}\t{row.Type}\t{row.Status}\t{row.Address ?? "--"}\t{row.LifeSeconds?.ToString() ?? "--"}");
if (names.Count == 0)
    Console.WriteLine("No names. The node line above is the Netbios code when the call failed.");

var hosts = NetworkHelper.GetLmHosts();
Console.WriteLine($"lmhosts={hosts.Count}");
foreach (var row in hosts)
    Console.WriteLine($"{row.Address}\t{row.Name}\tpre={row.Preload}\tdom={row.Domain ?? "--"}\tmh={row.MultiHome}\tinclude={row.IncludePath ?? "--"}");
if (hosts.Count == 0)
    Console.WriteLine("No LMHOSTS rows. A missing file is normal.");
