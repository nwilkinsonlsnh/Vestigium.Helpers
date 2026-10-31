# Vestigium.Helpers.Network.RawSocket

Elevated raw-socket probes. SYN scan first.

This package needs admin on Windows and `CAP_NET_RAW` on Linux. A missing right is a typed denial. It does not spawn a process.

`Vestigium.Helpers.Network` stays connect-scan only. A host that does not need SYN does not reference this package.

## Identity

| Field | Value |
|---|---|
| Package | `Vestigium.Helpers.Network.RawSocket` 1.0.0 |
| TFM | `net10.0` |
| Privilege | Admin / `CAP_NET_RAW` |
| License | MIT |

## Consume

```xml
<PackageReference Include="Vestigium.Helpers.Network.RawSocket" Version="1.0.0" />
```

```csharp
using Vestigium.Helpers.Network;
using Vestigium.Helpers.Network.RawSocket;

var access = RawSocketRights.Check();
var scan = await RawSocketHelper.SynScan("192.0.2.1", PortSet.FromList([22, 80, 443])).RunAsync();
```

## Surface

| Call | Returns | Notes |
|---|---|---|
| `RawSocketRights.Check` | `RawSocketAccess` | Administrator on Windows. `CapEff` bit 13 on Linux. |
| `RawSocketHelper.SynScan` | `RawSocketJob<SynScanResult>` | Hosts you supply. `PortSet`. Cap 64 hosts, 1024 ports. Half-open. IPv4. |

`SynScan` sends a SYN. A SYN-ACK is open. A RST is closed. A timeout is filtered. An open port gets a RST so the handshake is not completed.

Missing rights, or a raw TCP socket the OS will not open, is `Denied`. On Windows a raw TCP send is often blocked even for an administrator. This package does not fall back to a connect scan.

## Rules that do not move

- No raw sockets in `Vestigium.Helpers.Network`. Connect scan stays there.
- No CIDR expansion. Hosts are supplied.
- No packet capture in this PR.
- No TLS. No HTTP.
- No process spawn.
