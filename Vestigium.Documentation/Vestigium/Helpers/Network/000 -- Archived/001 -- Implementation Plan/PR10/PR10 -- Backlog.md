# Vestigium.Helpers.Network — PR10 Backlog

**Document ID:** VEST-HLP-NETWORK-PR10-BL  
**Package:** `Vestigium.Helpers.Network` **1.1.0** on the tree. This release is **1.2.0**. Do not republish 1.1.0 as if it had these doors.  
**Repo path:** `Vestigium.Documentation/Vestigium/Helpers/Network/001 -- Implementation Plan/PR10/`  
**Binding:** `Requirements_v1.6.md` wins until PR10-10 amends it. A row here is in the release only when the implementation plan says so.

## Intent

PR10 is one release with two lanes. Same library. Same façade. No new package.

- **Lane A** makes the 1.1.0 doors tell the truth.
- **Lane B** adds three jobs that fit the library and do not reopen locked denies.

Lane A ships before Lane B. A Lane B job that needs an honest echo or an honest neighbor waits for that A slice.

## What 1.1.0 already is

| Slice | State |
| :--- | :--- |
| Bind | Validate + socket `Apply`. BCL `Ping` is still unbound. |
| Family pin | On trace and pathping. Unset is legacy resolve. |
| Pathping | Walk then ICMP sample. Link loss clamped at 0. |
| PTR | One lookup per hop. Miss is empty. |
| TCP probe | Silent UDP falls through to connect. Intermediate hops often stay `*`. |
| TcpConnect | One host, one port. |
| Counters | Start/end delta. Optional interval. No bill. |
| Window duration | On `EchoWindow`. Old recipes stay count-only. |
| PMTU | DF echo, binary search. Timeout is treated like "too big." |
| ProbeNeighbor | Windows IPv4 `SendARP`. Else one row from the full table. |
| Campaign recipe | Echo options persist. Bind fields do not. |
| Package | **1.1.0**. Events through 14560. |

## In this release — Lane A

| ID | Item | Why it is in |
| :--- | :--- | :--- |
| PR10-01 | Bound ICMP echo. When `InterfaceIndex` or `SourceAddress` is set, the echo job uses a bound ICMP path. Unbound echo may keep BCL `Ping`. | 1.1.0 advertised bind on echo and did not send on that NIC. |
| PR10-02 | PMTU status split. `PacketTooBig` / DF fail means shrink. Timeout means unknown, not smaller. Walk only shrinks on a sized reject. | A dead host must not report MTU 1. |
| PR10-03 | Neighbor resolve for one address. Windows `ResolveIpNetEntry2` (or equal). Linux one-address netlink/ND. `GetNeighbors` stays the table dump. `ProbeNeighbor` must not scan the table to find the row as its primary path. | 1.1.0 is a filter of the dump plus `SendARP` on IPv4. |
| PR10-04 | Pathping phase 2 samples with the probe protocol the walk settled on. ICMP walk → ICMP sample. UDP walk → UDP sample. TCP walk → TCP sample. | Sampling a TCP-found hop with ICMP measures a different path. |
| PR10-05 | Echo campaign recipe persists `interfaceIndex` and `sourceAddress` when the caller set them. Open of an old recipe leaves both unset. | Bind on the live options is useless if Open drops it. |

## In this release — Lane B

| ID | Item | Why it is in |
| :--- | :--- | :--- |
| PR10-06 | `UdpProbe`. One host, one port. Result is replied, unreachable, or timed out, plus elapsed ms. No port list. | Sister of `TcpConnect`. ICMP and TCP are not the only filtered protocols. |
| PR10-07 | `ProbeDns`. One name, one server (or the OS path). Result is answered, refused, or timed out. Uses the existing DNS door and bind. Not HTTP. | `LookupAsync` is a lookup. Hosts also need "does this server talk DNS." |
| PR10-08 | `WatchAdapter`. One adapter, one duration, optional interval. Samples `OperationalStatus` and speed. Does not plot. Does not bill. | Counters say bytes. They do not say the NIC went down. |

## Paper and gate

| ID | Item | Why it is in |
| :--- | :--- | :--- |
| PR10-09 | Amend SRS, Design, Guide, and the package README in place. csproj and README consume line **1.2.0**. | 1.1.0 does not contain Lane B doors or the A truth fixes. |
| PR10-10 | `dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network` | Owner gate on the clone. |

Test methods are named for the behavior. They do not use the release id.

## Out of this release

| Item | Why not |
| :--- | :--- |
| Plot API | Never this library. |
| Packed IEEE OUI | Lookup stays a URL on request. |
| Default-route write | Decision 34 stands. `0.0.0.0/0` and `::/0` stay `NetworkRouteDenied`. |
| Port sweep / port set / staggered multi-port | Decision 41 and the PR09 backlog stand. One connect is the job. A list of ports is a later addendum, not PR10. |
| HTTP reachability client | The only HTTP remains the OUI GET. |
| Packet capture | Not a diagnostic job. |
| Scheduler install | A different package. |
| Banner grab, payload past handshake | TcpConnect and UdpProbe stay handshake / one datagram. |
| Linux NetBIOS, netplan writers, repo Linux CI | Unchanged. |

## Defaults this release locks

| Setting | Value |
| :--- | :--- |
| Package | **1.2.0**. 1.1.0 stays the prior publish. |
| EVENTID | 14500–14999, count by 5, new ids from **14565**, assigned on the slice that logs them |
| Route write | Option C. Defaults denied. |
| OUI | Caller URL on request. Snapshot not grown. |
| Tests | `FullyQualifiedName~Network`. No public Internet. No ProgramData and no `/var/lib/vestigium` except a test root. |
| Commit | `Network PR10: <id short goal>` |

## Close gate

1. PR10-01 through PR10-09 on `main` as scoped.
2. PR10-10 green on the clone.
3. No plot API. No packed IEEE registry. No default-route write. No port sweep. No process spawn.
4. No packet bytes, credentials, or `Exception` object in log lines.
5. Unbound BCL `Ping` remains legal only when bind options are omitted.
