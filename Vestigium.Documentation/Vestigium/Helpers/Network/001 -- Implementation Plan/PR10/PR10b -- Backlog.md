# Vestigium.Helpers.Network — PR10b Backlog

**Document ID:** VEST-HLP-NETWORK-PR10B-BL  
**Package:** still **1.2.0**. This is not a new product version. Do not bump to 1.2.1 unless PR10-10 already shipped 1.2.0 as published NuGet.  
**Repo path:** `Vestigium.Documentation/Vestigium/Helpers/Network/001 -- Implementation Plan/PR10/`  
**Binding:** `Requirements_v1.6.md`. Decisions 34 and 41 stay denied.

## Intent

PR10 landed the doors. Four of them can still lie. PR10b makes those four true before the owner gate. No new façade names. No sweep. No default-route write.

## In this pass

| ID | Item | Why it is in |
| :--- | :--- | :--- |
| PR10b-01 | Neighbor resolve uses a real `MIB_IPNET_ROW2` / `SOCKADDR_INET` layout (or documented IP Helper wrappers), not a guessed 128-byte blob. IPv6 returns a MAC when the stack has one. | PR10-03 can miss a neighbor that `GetNeighbors` already shows. |
| PR10b-02 | Bound ICMP send path is the pinned NIC. After `EgressBind.Apply`, the socket local address is the source (when set) or an address on `InterfaceIndex`. A mismatch is Failed, not a quiet stack pick. | PR10-01 can still let the OS choose. |
| PR10b-03 | Pathping UDP and TCP samples call `EgressBind.Apply` with the pathping bind options. `UdpProbeAsync` grows bind parameters or the sample uses `UdpProbe` / `TcpConnect` which already bind. | PR10-04 samples the protocol and forgets the NIC. |
| PR10b-04 | Restore decisions 1–45 in `Requirements_v1.6.md`. PR10-09 condensed the lock table to “1–45 stand.” The lock file must hold the locks. | A later reader cannot implement from a summary. |
| PR10b-05 | `dotnet test src/Vestigium.Helpers.Tests --filter FullyQualifiedName~Network` | Same owner gate as PR10-10. |

## Out of this pass

| Item | Why not |
| :--- | :--- |
| New job names | Those are PR11. |
| Port set / stagger | Decision 41. |
| Default-route write | Decision 34. |
| HTTP reachability | Decision 31. |
| Plot / scheduler | Unchanged. |
| Version bump to 1.2.1 | Only if 1.2.0 is already a published package. |

## Close gate

1. PR10b-01 through PR10b-04 on `main` as scoped.
2. PR10b-05 green on the clone.
3. No new public type unless a test needs an internal helper.
