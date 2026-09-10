# Vestigium.Helpers.Network — Requirements Specification

**Document ID:** VEST-HLP-NETWORK-SRS-000  
**Version:** 1.0  
**Status:** Draft. Replaces the 7 September 2026 skeleton. Awaiting acceptance before the public API grows past Identity + Probe.  
**Date:** 9 September 2026  
**Package:** `Vestigium.Helpers.Network`  
**TFM:** `net10.0` (not Windows-only; Windows-gated verbs are marked)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)  
**Hosts:** PingIQ, DnsIQ, TraceIQ, HttpIQ, ProbeHost, and any other Vestigium diagnostic host

If implementation and this file disagree, this file wins.

This library is not `ping.exe`, `tracert.exe`, `netstat.exe`, `nbtstat.exe`, `arp.exe`, `netsh.exe`, or `nslookup.exe`. Those tools are the *behavior reference* for reachability, path, sockets, NetBIOS, neighbors, interface configuration, and DNS. Network is the *validated job*: the same questions, structured results a host can bind, Pause/Cancel on long probes, and an ALCOA+ record written through `Vestigium.Logging`. It does not take administrative rights for read operations. It does not configure the machine in v1.

---

## 0. How to read this document

It records:

- one façade (`NetworkHelper`) for workstation inventory and diagnostic jobs
- one job engine shared by Ping, Trace, and long DNS checks: start, live progress, Cancel, finalize
- a workstation snapshot: adapters, addresses, DHCP, DNS, gateways, NetBIOS-over-TCP
- CLI cousins as the acceptance oracle, not as a process to spawn
- `netsh` in v1 as a **read-only informational snapshot**, not a configuration shell
- HelperLog only; Category `Helpers`; APPID `Network`; registered Network subcategories
- no HTTP client product in v1 (HttpIQ consumes this library; it does not live here yet)
- no firewall, proxy, or Wi-Fi profile mutation in v1
- optional Analytics handoff at finalize for RTT / hop-time NumericSeries

---

## 1. Purpose

Give every Vestigium host one way to ask “what does this workstation think the network is?” and “can it reach that name or address?” so an operator in a GxP / GMP setting can show *what was intended* and *what happened*.

```csharp
var snap = NetworkHelper.GetWorkstation();          // adapters, DHCP, DNS, gateways
var ping = NetworkHelper.Ping("8.8.8.8", new PingJobOptions { Count = 4 });
ping.ProgressChanged += (_, p) => ui.Render(p);
var result = await ping.RunAsync(cancellation);

var dns = await NetworkHelper.LookupAsync("example.com", new DnsLookupOptions
{
    Server = IPAddress.Parse("1.1.1.1"),
    RecordType = DnsRecordType.A
});
```

This is not FileIo. FileIo still moves trees. This is not Processes. Processes still launches other executables. Network must not become a wrapper that shells out to `ping.exe`.

---

## 2. Decisions locked in this version

| # | Decision | Locked as |
|---|---|---|
| 1 | Engine | BCL first: `System.Net.NetworkInformation`, `System.Net.Sockets`, `System.Net.Dns` where it is enough. Explicit nameserver queries use UDP/TCP 53 (RFC 1035 subset in-process). No `cmd.exe` / `ping.exe` / `tracert.exe` / `nslookup.exe` spawn in v1. |
| 2 | TFM | `net10.0`. Not Windows-only. Windows-only verbs throw `PlatformNotSupportedException` after HelperGuard Failed. |
| 3 | Product | Workstation inventory + diagnostic jobs (Ping, Trace, Listeners/Connections, Neighbors/ARP, NetBIOS, DNS lookup, read-only interface snapshot). Not an HTTP stack. Not a configuration tool. |
| 4 | CLI cousins | Behavior reference and operator-facing text renderer only. Primary API returns typed objects. A host may print a ping-like transcript; the transcript is not the contract. |
| 5 | `netsh` | **Read-only informational snapshot** in v1 (`interface`, IPv4/IPv6 addresses, DNS, routes, neighbors, optional WLAN radio/state). No `netsh interface ip set`, no firewall, no winhttp set, no profile add/delete. |
| 6 | Mutations | **Not v1:** ARP `-s`/`-d`, nbtstat `-R`/`-RR` cache reload, adapter enable/disable, static IP, DNS server write. Read tables only. |
| 7 | Logging door | `HelperLog` only. Category = `Helpers`. APPID = `Network`. Library never calls `VestigiumLogger.Initialize`. |
| 8 | Log contents | Hosts, addresses, adapter names, hop counts, RTT ms, record types, result codes. **Never** packet payloads, credentials, or `Exception` objects. |
| 9 | Jobs | Ping and Trace are `NetworkJob` instances: `RunAsync`, Cancel, live progress. One-shot inventory methods are synchronous (or short async) and do not use the job engine. |
| 10 | Continuous Ping | `Count = 0` means “until cancelled” (CLI `-t`). Must be cancellable. Cap optional `MaxDuration`. Default finite Count = **4**. |
| 11 | DNS server | Lookup **must** accept an explicit nameserver. Omitted server = OS resolver (`System.Net.Dns` / configured adapter DNS). Specified server = RFC 1035 query to that address, port 53. |
| 12 | IPv4 and IPv6 | First-class. Family may be forced (`Unspecified`, `IPv4`, `IPv6`). Dual-stack adapters report both. |
| 13 | Privileges | Read inventory and ICMP/DNS/TCP probes run without elevation. Fields the OS withholds are null + `Limited` flag, not a crash. Elevation is never requested by the library. |
| 14 | Probe | In-memory / loopback / `%TEMP%` only. Must not send packets off-box. Must not write the Desktop. |
| 15 | Analytics | Network is the **host**. Ping and Trace accumulate RTT (and per-hop RTT) and at Finalize may construct `NumericSeries` via `Vestigium.Helpers.Analytics`. Empty series are null, never NaN. Charting stays in Charts. |
| 16 | Text render | Optional `ToTranscript()` on results mimics the familiar CLI enough that an operator recognizes it. It is derived from the structured result. Do not parse CLI stdout to build the result. |
| 17 | Timeouts | Reject non-positive timeouts (`HelperGuard`). Defaults in §5. Do not hang a host. |
| 18 | Siblings | May call Analytics at finalize. Must not reference Charts, FileIo, Csv, ClosedXml, Encryption, WinReg (v1). Processes is not used to shell tools. |

---

## 3. Goals

**G1.** One façade (`NetworkHelper`) owns Identity, Probe, inventory, and job factories.  
**G2.** One workstation snapshot answers adapter, address, DHCP, DNS, gateway, MAC, and NetBIOS-over-TCP questions without the host talking to WMI itself.  
**G3.** Ping, Trace, and explicit-server DNS are first-class and cancellable.  
**G4.** Netstat-class socket and statistics views are structured tables, not a text dump.  
**G5.** ARP / neighbor table is readable on the current platform.  
**G6.** NetBIOS name table and cache are readable on Windows.  
**G7.** DNS lookup can target a specified server and a specified record type.  
**G8.** `netsh`-class interface configuration is a read-only snapshot.  
**G9.** Logs meet ALCOA+ through HelperLog. Category `Helpers`, APPID `Network`.  
**G10.** `Probe` stays on-box.  
**G11.** At finalize, Ping/Trace may publish Analytics snapshots of RTT so PingIQ / TraceIQ do not invent statistics.

---

## 4. Workstation inventory

### 4.1 Snapshot

`NetworkHelper.GetWorkstation()` returns `WorkstationNetwork` captured at call time. It is a point-in-time clone, not a live subscription. A host that needs refresh calls again.

```text
WorkstationNetwork
  HostName, DomainName, NodeType
  CapturedUtc
  Adapters[]
  DnsSuffixSearchList[]
  DefaultRoutes[]          // IPv4 / IPv6 default gateways actually in the route table
  IsInternetLikely         // best-effort: any adapter Up + a default gateway. Not a WAN probe.
```

### 4.2 Adapter record

Every adapter the OS enumerates is included (physical, virtual, tunnel, loopback). Hosts filter; the library does not drop loopback by default. An option `IncludeDown` default **true** keeps disabled NICs visible.

| Field | Notes | CLI / API cousin |
|---|---|---|
| `Id` | Stable OS identifier (GUID on Windows) | `NetworkInterface.Id` |
| `Name` | Connection name (“Ethernet”, “Wi-Fi”) | Adapter Name |
| `Description` | Driver / device description | Description |
| `Type` | Ethernet, Wireless80211, Loopback, Tunnel, Unknown | `NetworkInterfaceType` |
| `OperationalStatus` | Up, Down, Testing, … | `netsh interface show interface` |
| `SpeedBps` | Nominal link speed; 0 if unknown | |
| `SupportsMulticast` | | |
| `MacAddress` | Canonical `AA-BB-CC-DD-EE-FF`; null if none | MAC Address |
| `UnicastAddresses[]` | Each: `Address`, `Family`, `PrefixLength` (CIDR), `SubnetMask` (IPv4 dotted; IPv6 null), `IsDnsEligible`, `IsTransient` | IP Address, CIDR, Subnet Mask |
| `Dhcp` | See §4.3 | IsDhcpEnabled + lease |
| `Gateways[]` | Default gateways advertised on this adapter | Default Gateway |
| `DnsServers[]` | Configured resolvers on this adapter, ordered | DNS Server(s) |
| `WinsServers[]` | Windows; empty elsewhere | WINS |
| `DnsSuffix` | Connection-specific suffix | |
| `NetbiosOverTcp` | `Enabled`, `Disabled`, `Default`, `Unknown` | NetbiosOverTcp |
| `Mtu` | If the OS exposes it | |
| `IsReceiveOnly` / `IsPointToPoint` | | |
| `BytesSent` / `BytesReceived` | IPv4+IPv6 statistics at capture time | `netstat -e` cousin |

CIDR (`PrefixLength`) and IPv4 subnet mask are both required when the address is IPv4. Derive one from the other if the OS only supplies one. Do not invent a `/24`.

### 4.3 DHCP block (per adapter)

Present when the adapter has IPv4 and the OS exposes DHCP state. Otherwise `IsDhcpEnabled = false` and the rest are null.

| Field | Notes |
|---|---|
| `IsDhcpEnabled` | IPv4 DHCP on this adapter |
| `DhcpServer` | Server that handed the lease |
| `LeaseObtainedUtc` | Windows; null if unknown |
| `LeaseExpiresUtc` | Windows; null if unknown |
| `IsExpired` | `true` when expires is in the past |

Lease timestamps are Windows-gated. On non-Windows, report `IsDhcpEnabled` when known and leave timestamps null.

### 4.4 NetBIOS-over-TCP

Per adapter: Enabled / Disabled / Default / Unknown.

Windows source of truth is the adapter NetBIOS option (IP Helper / `Tcpip` interface). Do not guess from the presence of an nbtstat cache.

### 4.5 Filtering helpers

```csharp
NetworkHelper.GetAdapters(NetworkAdapterQuery? query = null);
NetworkHelper.GetAdapter(string nameOrId);
```

`NetworkAdapterQuery`: `UpOnly`, `ExcludeLoopback`, `ExcludeTunnel`, `Family`.

Unknown name: HelperGuard Failed then throw. Missing-but-legal empty list (no adapters match filter) is empty, not an exception.

---

## 5. Diagnostic jobs

### 5.1 Shared job shape

```
Start
  → Pending (recipe logged)
  → Running (probes in flight; progress pumped)
  → Cancel requested → drain current probe → Finalize Cancelled
  → Complete → Finalize Success / Failed
```

```csharp
public sealed class NetworkJob<TResult>
{
    public string JobId { get; }
    public NetworkVerb Verb { get; }
    public NetworkProgress Progress { get; }
    public event EventHandler<NetworkProgress>? ProgressChanged;
    public Task<TResult> RunAsync(CancellationToken cancellation = default);
    public void Cancel();
}
```

Live progress is the bar. JSONL is the record. Same two-channel rule as FileIo (§9).

### 5.2 Ping (ICMP)

Behavior reference: `ping`.

| Option | Default | CLI cousin | Notes |
|---|---|---|---|
| `Target` | required | destination | Hostname or IP. Hostnames resolve first. |
| `Count` | 4 | `-n` | `0` = until cancelled (`-t`) |
| `Timeout` | 4 s | `-w` | Per echo. Range 10 ms–60 s |
| `BufferSize` | 32 | `-l` | 1–65500. Reject 0 |
| `Ttl` | OS default | `-i` | |
| `DontFragment` | false | `-f` | IPv4 only |
| `Family` | Unspecified | `-4` / `-6` | |
| `SourceAddress` | null | `-S` | Bind local address when set |
| `ResolveAddress` | false | `-a` | Reverse-lookup reply address |
| `Interval` | 1 s | (Unix `-i`) | Delay between echoes. 0 allowed |
| `MaxDuration` | null | | Hard stop for continuous mode |

Each echo produces `PingReply`:

| Field | Notes |
|---|---|
| `Sequence` | 1-based |
| `Status` | Success, TimedOut, DestinationUnreachable, TtlExpired, HardwareError, Unknown |
| `Address` | Responder, if any |
| `RoundtripTime` | `TimeSpan`; null on timeout |
| `Ttl` | From reply, if present |
| `BufferLength` | |
| `DontFragmentObserved` | when known |

Job result `PingResult`:

- Target, resolved address(es), family used
- Replies[]
- `Sent`, `Received`, `Lost`, `LossPercent`
- `RoundtripMin` / `Max` / `Average` / `StdDev` over successful replies only
- `StartedUtc`, `EndedUtc`
- Optional `Stats` Analytics snapshot (`rtt-ms`) when n ≥ 1 success

Transcript example (derived):

```text
Pinging 8.8.8.8 with 32 bytes of data:
Reply from 8.8.8.8: bytes=32 time=14ms TTL=117
...
Packets: Sent = 4, Received = 4, Lost = 0 (0% loss)
```

Resolve failure before the first echo: job Failed, zero replies, reason `NameResolution`.

ICMP may be blocked by policy. That is `TimedOut` / `DestinationUnreachable`, not an unhandled exception.

### 5.3 Trace (TTL walk)

Behavior reference: `tracert` / `traceroute`.

| Option | Default | CLI cousin |
|---|---|---|
| `Target` | required | destination |
| `MaxHops` | 30 | `-h` |
| `Timeout` | 4 s | `-w` |
| `ProbesPerHop` | 3 | Windows tracert sends 3 |
| `ResolveHostNames` | true | inverse of `-d` |
| `Family` | Unspecified | `-4` / `-6` |
| `SourceAddress` | null | `-S` |

Implementation: successive ICMP (or UDP-to-unused-port where ICMP is unavailable) with increasing TTL. Do not spawn `tracert.exe`.

Each hop `TraceHop`:

| Field | Notes |
|---|---|
| `Hop` | 1-based TTL |
| `Address` | null if all probes timed out (`* * *`) |
| `HostName` | if resolution requested and succeeded |
| `Probes[]` | per-probe RTT or timeout |
| `Status` | Reached, TtlExpired, TimedOut, Unreachable |

Job completes when a probe reaches the target, `MaxHops` is hit, or destination is reported unreachable.

`TraceResult`: hops, reached yes/no, total time, optional Analytics per-hop RTT series.

### 5.4 DNS lookup (nslookup-class)

Behavior reference: `nslookup` **non-interactive** mode. No REPL in v1.

| Option | Default | Notes |
|---|---|---|
| `Name` | required | QNAME. PTR accepts an IP and flips to reverse form |
| `Server` | null | null = OS resolver; else query that nameserver |
| `RecordType` | `A` | A, AAAA, CNAME, MX, NS, PTR, SOA, TXT, SRV, ANY |
| `Timeout` | 3 s | |
| `Retries` | 2 | After the first try |
| `UseTcp` | false | TCP 53 fallback when truncated (TC bit) is automatic even if false |
| `RecursionDesired` | true | RD bit |
| `Family` | Unspecified | When connecting to `Server` |

`DnsLookupResult`:

- Question (name, type, class IN)
- Server used (OS or explicit address)
- `ResponseCode` (NoError, NxDomain, ServFail, Refused, Timeout, …)
- Answers[] / Authorities[] / Additionals[] as `DnsRecord` (`Name`, `Type`, `Ttl`, typed data)
- `Elapsed`
- `Truncated`, `RecursionAvailable`

Typed data:

| Type | Data fields |
|---|---|
| A / AAAA | `IPAddress` |
| CNAME / PTR / NS | `HostName` |
| MX | `Preference`, `Exchange` |
| TXT | `Text` (concatenated strings) |
| SOA | `MName`, `RName`, `Serial`, `Refresh`, `Retry`, `Expire`, `Minimum` |
| SRV | `Priority`, `Weight`, `Port`, `Target` |

Multiple lookups (A+AAAA) are two calls or `RecordType.Any` — do not silently merge.

“Check this DNS server” is this API with `Server` set. A host that wants “compare OS vs 8.8.8.8 vs 1.1.1.1” loops. The library may offer `LookupMany(name, IReadOnlyList<IPAddress> servers)` as sugar that returns one result per server. Same options otherwise.

### 5.5 Connections and statistics (netstat-class)

Behavior reference: `netstat`. Read-only.

```csharp
NetworkHelper.GetConnections(NetworkConnectionQuery? query = null);
NetworkHelper.GetTcpStatistics();
NetworkHelper.GetUdpStatistics();
NetworkHelper.GetIPv4Statistics();
NetworkHelper.GetIPv6Statistics();
NetworkHelper.GetRoutes();
```

`NetworkConnection` (TCP / UDP):

| Field | netstat cousin |
|---|---|
| `Protocol` | TCP, TCPV6, UDP, UDPV6 |
| `LocalAddress`, `LocalPort` | |
| `RemoteAddress`, `RemotePort` | `*` / 0 for listeners / UDP |
| `State` | LISTENING, ESTABLISHED, TIME_WAIT, … UDP = null |
| `ProcessId` | `-o`; null if the OS withholds it |
| `ProcessName` | `-b` cousin; best-effort, null without rights |
| `ScopeId` | IPv6 |

`NetworkConnectionQuery`: protocol, state, local/remote port, listening-only (`-a` listening+established is default when omitted: return all the OS gives). `Numeric = true` means do not reverse-lookup (default **true**; names are opt-in because they are slow).

`GetRoutes()` is the `netstat -r` / `route print` cousin: destination, mask/prefix, gateway, interface, metric. Read-only.

Process names require a process lookup. Use `System.Diagnostics.Process` by PID. Do not take a dependency on `Vestigium.Helpers.Processes` in v1. Failure to resolve a name leaves `ProcessName` null.

### 5.6 Neighbors / ARP

Behavior reference: `arp -a` / `netsh interface ipv4 show neighbors`.

```csharp
NetworkHelper.GetNeighbors(NetworkNeighborQuery? query = null);
```

`NetworkNeighbor`:

| Field | Notes |
|---|---|
| `Address` | IPv4 or IPv6 |
| `MacAddress` | may be null for incomplete |
| `InterfaceId` / `InterfaceName` | `-N if_addr` cousin |
| `State` | Reachable, Stale, Incomplete, Permanent, Unknown |
| `IsRouter` | when known |
| `Family` | |

Filter by interface name or IP. Empty table is empty, not Failed.

**Not v1:** `arp -s`, `arp -d`, flush.

### 5.7 NetBIOS (nbtstat-class) — Windows-only

Behavior reference: `nbtstat`.

| Method | nbtstat cousin | Notes |
|---|---|---|
| `GetLocalNetbiosNames()` | `-n` | Local name table |
| `GetNetbiosCache()` | `-c` | Remote name cache |
| `QueryNetbiosName(string name)` | `-a` | Remote name table by NetBIOS name |
| `QueryNetbiosAddress(IPAddress ip)` | `-A` | Remote name table by IP |
| `GetNetbiosSessions()` | `-s` / `-S` | Sessions; resolve flag on options |

`NetbiosNameEntry`: `Name`, `Suffix` (00 workstation, 20 file service, …), `Type` (Unique / Group), `Status` (Registered, Conflict, …), `IpAddresses[]`.

Non-Windows: HelperGuard Failed + `PlatformNotSupportedException`.

**Not v1:** `-R` / `-RR` purge and reload (mutation).

### 5.8 Interface snapshot (netsh-class, read-only)

Behavior reference: selected **show** commands only.

```csharp
NetworkHelper.GetInterfaceSnapshot();
```

`InterfaceSnapshot` aggregates what §4 already has, plus:

| Block | netsh cousin | v1 |
|---|---|---|
| Interfaces admin/connect state | `interface show interface` | Yes |
| IPv4 / IPv6 config | `interface ip show config`, `interface ipv6 show address` | Yes (same data as adapters) |
| DNS | `interface ip show dns` | Yes |
| Routes | `interface ipv4/ipv6 show route` | Yes (`GetRoutes`) |
| Neighbors | `interface ipv4/ipv6 show neighbors` | Yes (`GetNeighbors`) |
| WLAN radio / connected SSID / signal / BSSID / auth | `wlan show interfaces` | Yes when a wireless adapter exists; fields null otherwise |
| WLAN profiles list (names only) | `wlan show profiles` | Yes, names only — **no keys, no XML** |
| Firewall | `advfirewall` | **Not v1** |
| WinHTTP proxy | `winhttp show proxy` | **Not v1** |
| Any `set` / `add` / `delete` | | **Not v1** |

This method exists so a host can say “netsh-style dump” and get one object. It must not open a netsh process.

---

## 6. Public surface (v1)

Names may move a token. The shapes may not.

```csharp
public static class NetworkHelper
{
    public static string Identity { get; }   // "Vestigium.Helpers.Network"
    public static string Probe();

    public static WorkstationNetwork GetWorkstation();
    public static IReadOnlyList<NetworkAdapter> GetAdapters(NetworkAdapterQuery? query = null);
    public static NetworkAdapter GetAdapter(string nameOrId);

    public static NetworkJob<PingResult> Ping(string target, PingJobOptions? options = null);
    public static NetworkJob<TraceResult> Trace(string target, TraceJobOptions? options = null);

    public static Task<DnsLookupResult> LookupAsync(string name, DnsLookupOptions? options = null,
        CancellationToken cancellation = default);
    public static Task<IReadOnlyList<DnsLookupResult>> LookupManyAsync(string name,
        IReadOnlyList<IPAddress> servers, DnsLookupOptions? options = null,
        CancellationToken cancellation = default);

    public static IReadOnlyList<NetworkConnection> GetConnections(NetworkConnectionQuery? query = null);
    public static IPStatistics GetTcpStatistics(AddressFamily family = AddressFamily.InterNetwork);
    public static IPStatistics GetUdpStatistics(AddressFamily family = AddressFamily.InterNetwork);
    public static IReadOnlyList<NetworkRoute> GetRoutes(AddressFamily? family = null);

    public static IReadOnlyList<NetworkNeighbor> GetNeighbors(NetworkNeighborQuery? query = null);

    public static IReadOnlyList<NetbiosNameEntry> GetLocalNetbiosNames();          // Windows
    public static IReadOnlyList<NetbiosCacheEntry> GetNetbiosCache();              // Windows
    public static IReadOnlyList<NetbiosNameEntry> QueryNetbiosName(string name);   // Windows
    public static IReadOnlyList<NetbiosNameEntry> QueryNetbiosAddress(IPAddress ip);
    public static IReadOnlyList<NetbiosSession> GetNetbiosSessions(bool resolveNames = false);

    public static InterfaceSnapshot GetInterfaceSnapshot();
}

public enum NetworkVerb { Ping, Trace, Lookup, Inventory }
public enum DnsRecordType { A, Aaaa, Cname, Mx, Ns, Ptr, Soa, Txt, Srv, Any }
public enum NetbiosOverTcp { Unknown = 0, Default = 1, Enabled = 2, Disabled = 3 }
```

`Probe()`:

- Logs Pending then Success
- Reads loopback adapter from the local inventory
- Must not ICMP off-box, must not query a public DNS server, must not write Desktop / ProgramData (except HelperLog if the host already initialized)

---

## 7. Options defaults

| Option | Default | Reject |
|---|---|---|
| Ping `Count` | 4 | `< 0` |
| Ping `Timeout` | 4 s | `< 10 ms` or `> 60 s` |
| Ping `BufferSize` | 32 | `< 1` or `> 65500` |
| Ping `Interval` | 1 s | `< 0` |
| Trace `MaxHops` | 30 | `< 1` or `> 255` |
| Trace `ProbesPerHop` | 3 | `< 1` or `> 10` |
| DNS `Timeout` | 3 s | `< 10 ms` or `> 30 s` |
| DNS `Retries` | 2 | `< 0` or `> 10` |

`HelperGuard` Failed then throw on reject. Do not clamp in silence.

---

## 8. Logging

Category = `Helpers`. APPID = `Network`.

Register these subcategories (reuse existing names where they already exist):

| Subcategory | When |
|---|---|
| Probe / Identity / Guard | Existing suite |
| Inventory | GetWorkstation, adapter count, Up/Down |
| Adapter | Single adapter read (name, status — not every address octet dump at Debug only) |
| Ping | Job start recipe, per-echo only on failure/timeout, summary |
| Trace | Job start, hop reached / `*`, summary |
| Dns | Server, QNAME, type, RCODE, answer count — not TXT body beyond length |
| Connection | GetConnections count, filter |
| Neighbor | GetNeighbors count |
| Netbios | Windows queries; name + suffix, not session payload |
| Route | GetRoutes count |
| Snapshot | GetInterfaceSnapshot |
| Job | Start / Cancel / Success / Failed |
| Stats | One Analytics line at Ping/Trace finalize |

Sparse audit:

- Job start: one `Job` Pending with the recipe (target, count, timeout, server)
- Quiet successful ping replies are **not** one JSONL line each
- Timeouts, unreachable, NxDomain, ServFail: one line
- Job end: one Success / Failed / Cancelled summary (`sent=4 recv=4 loss=0 min=12 max=18 avg=14`)
- Inventory: one line per snapshot (`adapters=7 up=3`)

Never: packet bytes, WLAN keys, TXT blob contents, `Exception` argument to HelperLog.

---

## 9. Progress — two channels

### 9.1 Live progress (the bar)

Ping / Trace push `NetworkProgress` as each probe completes (and at least every 250–500 ms during a wait):

- `Phase`, `Verb`, `Current` / `Total` (echo number or hop)
- `LastStatus`, `LastRoundtrip`
- `IsCancelled`

This is for a screen. It is not the regulated record.

### 9.2 Audit log (the record)

JSONL through HelperLog. Sparse. See §8.

---

## 10. Demo

`Vestigium.Helpers.Network.Demo` stays on `HelperWpfHost` + APPID `Network` until this SRS is Accepted. After the implementation phases it becomes a shipped gallery:

- Overview (Identity, Probe, host name)
- Adapters (table of §4.2 fields, DHCP expander)
- Ping (target, count, live RTT list, transcript pane)
- Trace (hop table)
- DNS (name, type, optional server)
- Connections (netstat table)
- Neighbors (ARP)
- NetBIOS (Windows; hidden or disabled on other OS)
- Snapshot (read-only netsh-class view)
- JSONL audit pane

Probe remains on-box. Gallery Ping/Trace/DNS against public targets is operator-initiated, never automatic on startup.

---

## 11. Tests

xUnit, serial logger collection, temp `LogDirectory`.

- Identity is `Vestigium.Helpers.Network`.
- Probe writes Pending then Success; no off-box packets.
- `GetWorkstation` returns at least the loopback adapter with an address.
- IPv4 unicast rows expose both `PrefixLength` and `SubnetMask`, and they agree.
- Ping `127.0.0.1` Count=1 succeeds on a normal CI agent; if ICMP is forbidden, the test asserts a typed status (not a crash).
- Ping options reject Count < 0 and Timeout < 10 ms via HelperGuard.
- Cancel of a Count=0 ping completes as Cancelled and does not hang the test (> 5 s fail).
- Lookup of `localhost` via OS resolver returns a loopback address.
- Lookup with an explicit server that is `127.0.0.1` where nothing listens yields Timeout or Refused, not an unhandled exception.
- `GetConnections` returns a list (may be empty in a sandbox) and never throws on a normal agent.
- NetBIOS methods throw `PlatformNotSupportedException` on non-Windows.
- Tests never touch the real Desktop or live ProgramData.
- No packet payload in captured HelperLog lines.
- Do not require the public Internet for CI. Off-box cases are explicit opt-in / live tests, not the default suite.

---

## 12. Non-goals (v1)

- Spawning `ping`, `tracert`, `netstat`, `nbtstat`, `arp`, `netsh`, `nslookup`, `pathping`, `ipconfig`
- HTTP / HTTPS client, HttpIQ protocol features, TLS inspection
- Changing IP, DNS, DHCP, routes, ARP entries, NetBIOS cache, adapter admin state
- Full `netsh` shell (firewall, winhttp, DHCP server, RAS, WFP, namespace scripts)
- WLAN profile passwords / XML
- Packet capture (pcap / npcap / ETW sniffing)
- Bandwidth tests, iperf, pathping
- SNMP
- Port scanning as a product (a single TCP connect helper may appear later; not a range scanner)
- Interactive nslookup REPL
- IPv6 routing advertisement daemon features
- Admin elevation prompts
- Charts UI (Analytics numbers only)

---

## 13. Roadmap

| Version | Item |
|---|---|
| **v1.0** | This document: inventory, Ping, Trace, DNS (explicit server), connections, routes, neighbors, Windows NetBIOS read, read-only interface snapshot, HelperLog, Probe |
| **v1.1** | HTTP reachability helper for HttpIQ (HEAD/GET status + timing only; no cookie jar, no browser). TCP connect timing to a single host:port |
| **v1.2** | Analytics series always-on for Ping/Trace; Compare two DNS servers side-by-side report type |
| **v1.3** | Optional Windows mutations behind an explicit `NetworkMutation` flag: ARP flush, DNS cache flush (`ipconfig /flushdns` cousin), adapter restart. Each call logs Warning and refuses unless the host passed the flag |
| **later** | pathping-class loss-per-hop, packet capture, WLAN profile details without secrets, winhttp proxy read |

Never here: being FileIo; being Processes; being the suite logger; being a general `netsh.exe` replacement that writes configuration.

---

## 14. Mapping — CLI tool → API

| Tool | v1 coverage | API |
|---|---|---|
| Adapter / `ipconfig` | Addresses, mask, CIDR, gateway, DHCP lease, DNS, MAC | `GetWorkstation` / `GetAdapters` |
| `ping` | Count, timeout, size, TTL, DF, family, source, continuous+cancel, stats | `Ping` |
| `tracert` | Max hops, timeout, resolve, family, 3 probes/hop | `Trace` |
| `netstat` | Connections, listening, PID, stats, routing table | `GetConnections`, statistics, `GetRoutes` |
| `nbtstat` | `-n -c -a -A -s` read | NetBIOS methods (Windows) |
| `arp` | `-a` read | `GetNeighbors` |
| `nslookup` | Non-interactive, type, explicit server | `LookupAsync` / `LookupManyAsync` |
| `netsh` | Show interface / IP / DNS / route / neighbor / WLAN state | `GetInterfaceSnapshot` |

---

## 15. Acceptance

This SRS is **Draft** until it is marked Accepted on `main`. Acceptance means:

1. This file and an updated Developers Guide live under `src/Vestigium.Helpers.Network/_Documentation/`.
2. §8 subcategories are registered in `HelperLog.CreateTaxonomy`.
3. A follow-up implementation PR can be reviewed against this text without inventing a `ping.exe` wrapper, a `netsh set` API, or an HTTP client.

Do not grow `NetworkHelper` past Identity + Probe until Status is Accepted.
