# Vestigium.Helpers.Network — PR09 Backlog

**Document ID:** VEST-HLP-NETWORK-PR09-BL  
**Package:** `Vestigium.Helpers.Network` 1.0.1 on the tree. This release is **1.1.0**. Do not republish 1.0.1 as if it had these doors.  
**Repo path:** `Vestigium.Documentation/Vestigium/Helpers/Network/001 -- Implementation Plan/PR09/`  
**Binding:** `Requirements_v1.6.md` wins until PR09-11 amends it. A row here is in the release only when the implementation plan says so.

## Intent

PR09 is a release. Ten network jobs that 1.0.1 does not have. Same library, same façade, no new package.

The order they matter:

1. Pathping.
2. Egress bind on echo, trace, and DNS.
3. One timed TCP connect.
4. TCP as a trace probe.
5. Adapter counter sample over a span.
6. Echo-campaign window duration.
7. PTR name on each hop.
8. PMTU walk.
9. Address-family pin on trace.
10. One-target neighbor probe.

Build order is not that list. Bind and the family pin land before pathping, because pathping uses both. The implementation plan holds that order.

## What 1.0.1 already is

| Slice | State |
| :--- | :--- |
| Façade | `NetworkHelper` — inventory, ICMP echo/trace, DNS, stack tables, Option C routes, echo + share campaigns, prefix math, MAC/OUI, bandwidth, P95, `BillPercentile(NumericSeries, double)` |
| Trace | ICMP or UDP. Hop address, status, RTT. No loss split. No name. No family pin. No egress bind. |
| Echo campaign | Clock window plus count. Recipe persists echo options. No window duration. |
| Statistics | `GetStatistics` is one reading. |
| Neighbors | `GetNeighbors` is the whole ARP/ND table. |
| TFM | `net10.0`. Test project is `net10.0-windows`. |
| EVENTID | 14500–14999, used through 14555, count by 5. Next free is 14560. |
| Plot | Never this library. |
| OUI | Caller URL, fetched on request. Embedded snapshot is a stub and is not grown. |

## In this release

| ID | Item | Why it is in |
| :--- | :--- | :--- |
| PR09-01 | `Pathping`. Walk the path, then sample every hop. Report RTT, loss at the hop, and loss on the link to the next hop. | `Trace` cannot say where packets are dropped. |
| PR09-02 | Egress bind. Optional interface index and source address on echo, trace, DNS, and pathping. Omit means the stack chooses. No guessed index `1`. | A multi-homed host otherwise records the wrong path. |
| PR09-03 | `TcpConnect`. One host, one port, elapsed time, and connected / refused / timed out. | ICMP is often dropped. This is not a scanner. |
| PR09-04 | TCP trace probe, plus a destination port, beside ICMP and UDP. | A path that eats ICMP and UDP still answers a handshake. |
| PR09-05 | Counter sample on one adapter for a duration. Bytes in, bytes out, errors, discards. Returns samples. Does not bill and does not plot. | `BillP95` has no producer inside this library. |
| PR09-06 | Echo window may carry a duration. Count remains. Both set means stop at whichever comes first. Old recipes with no duration stay count-only. | A window today cannot say "run for 60 seconds." |
| PR09-07 | PTR on each trace and pathping hop. A miss leaves the name empty and does not fail the hop. | The address is already there. The name is a second call the host should not have to remember. |
| PR09-08 | PMTU walk. Largest echo payload that passes with don't-fragment set. | Echo can set the flag. Nothing walks the size. |
| PR09-09 | Address-family pin on trace and pathping. Unset keeps today's resolve. | A dual-stack host must be able to force IPv6. |
| PR09-10 | One-target neighbor probe. One address in, MAC or none out. The full-table dump stays. | The table dump is the wrong tool for one silent instrument. |
| PR09-11 | Amend SRS, Design, Guide, and the package README. csproj `Version` **1.1.0**. | The release is not shipped until the paper matches the doors. |
| PR09-12 | `dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network` | Owner gate on the clone. |

Test methods are named for the behavior. They do not carry the release id.

## Out of this release

| Item | Why not |
| :--- | :--- |
| Plot API | Never this library. |
| Packed IEEE OUI | The registry changes. Lookup stays a URL on request. |
| Default-route write | Still not offered. |
| Scheduler install, cron, schtasks, systemd | A different package. This library runs when the host calls it. |
| Packet capture | Not a diagnostic job. |
| Port scan, multi-port sweep | One connect is the job. A sweep is not. |
| HTTP client | The only HTTP in this library remains the OUI GET. |
| Linux NetBIOS, netplan writers, live Ubuntu mutate, repo Linux CI | Unchanged from 1.0.1. |

## Defaults this release locks

| Setting | Value |
| :--- | :--- |
| Package | **1.1.0**. 1.0.1 stays the prior publish. |
| EVENTID | 14500–14999, count by 5, new ids from 14560, assigned on the slice that logs them |
| Route write | Option C. Defaults denied. |
| OUI | Caller URL on request. Snapshot not grown. |
| Tests | `FullyQualifiedName~Network`. No public Internet. No ProgramData and no `/var/lib/vestigium` except a test root. |
| Commit | `Network PR09: <id short goal>` |

## Close gate

1. PR09-01 through PR09-11 on `main` as scoped.
2. PR09-12 green on the clone.
3. No plot API. No packed IEEE registry. No default-route write. No process spawn.
4. No packet bytes, credentials, or `Exception` object in log lines.
