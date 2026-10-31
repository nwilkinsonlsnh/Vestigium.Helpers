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

## Rules that do not move

- No raw sockets in `Vestigium.Helpers.Network`.
- No CIDR expansion.
- No packet capture in this PR.
- No TLS. No HTTP.
- No process spawn.
