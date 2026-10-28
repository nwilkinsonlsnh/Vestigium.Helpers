# Vestigium.Helpers.Network — Subnet calculator (locked)

**Document ID:** VEST-HLP-NETWORK-SRS-SUBNET-000  
**Version:** 1.3 addendum  
**Status:** Locked. Implements as Phase 9.  
**Date:** 10 September 2026  
**Parent:** [`Requirements_v1.0.md`](Requirements_v1.0.md) v1.3

If code and this file disagree, this file wins.

## 1. Locks

| # | Lock |
|---|---|
| S1 | Pure math. No `ipcalc`, `sipcalc`, `ipv6calc`, `subnet`. |
| S2 | IPv4 and IPv6 are both required in Phase 9. Same façade. |
| S3 | CIDR is the contract. Class A/B/C/D/E is a **label** on an address, never the mask. |
| S4 | IPv6 has no class. `TraditionalClass` is `None`. Scope labels still apply. |
| S5 | Usable IPv4 hosts exclude network + broadcast except `/31` (2 usable) and `/32` (1 usable). |
| S6 | `/31` and `/32` are first-class. Not errors. |
| S7 | Host/network asks pick the **smallest** prefix that satisfies the ask and still fits the parent. |
| S8 | If the parent cannot satisfy the ask → HelperGuard + throw. No silent shrink. |
| S9 | Listing is capped (`MaxList`, default 1024). Always return `TotalNetworks`. |
| S10 | Mask and prefix on IPv4 **agree** (reuse `Ipv4Prefix`). |
| S11 | HelperLog subcategory `Subnet`. Query + summary only. No address dumps. |
| S12 | Tests are on-box numbers only. No disk under ProgramData. |

## 2. Classful label (IPv4 only)

First octet, RFC 790 / traditional teaching table. This does **not** choose a mask.

| First octet | Class | Notes |
|---|---|---|
| 0–127 | A | Includes 127. Also set `Kind = Loopback` when 127/8. |
| 128–191 | B | |
| 192–223 | C | |
| 224–239 | D | Multicast. `Kind = Multicast`. |
| 240–255 | E | Reserved/experimental. |

Also set `Kind` independently of class:

`Unspecified`, `Loopback`, `Rfc1918`, `LinkLocal`, `Cgnat` (100.64/10), `Documentation` (TEST-NET-1/2/3, 192.0.0.0/24 docs), `Benchmark`, `Multicast`, `Broadcast`, `Unicast`, `CarrierGradeNat`.

API:

```
NetworkHelper.ClassifyAddress("192.168.10.37")
NetworkHelper.ClassifyAddress("224.0.0.1")
NetworkHelper.ClassifyAddress("2001:db8::1")
```

IPv6 `Kind`: `Unspecified`, `Loopback`, `LinkLocal`, `UniqueLocal` (fc00::/7), `Multicast`, `Documentation` (2001:db8::/32), `GlobalUnicast`, `Ipv4Mapped`.

## 3. Public surface

```
ClassifyAddress(string address) -> AddressClass
DescribePrefix(string cidr) -> PrefixBlock
DescribePrefix(string address, int prefixLength) -> PrefixBlock
DescribePrefix(string address, string dottedMask) -> PrefixBlock   // IPv4 only
PlanByHosts(string parentCidr, int minimumHosts, SubnetQuery? q = null) -> PrefixPlan
PlanByNetworks(string parentCidr, int minimumNetworks, SubnetQuery? q = null) -> PrefixPlan
SplitPrefix(string parentCidr, int childPrefix, SubnetQuery? q = null) -> PrefixPlan
SplitPrefixByCount(string parentCidr, int count, SubnetQuery? q = null) -> PrefixPlan
PackVlsm(string parentCidr, IReadOnlyList<int> hostNeeds, SubnetQuery? q = null) -> PrefixPlan
Contains(string prefixCidr, string address) -> bool
Overlaps(string leftCidr, string rightCidr) -> bool
Summarize(IEnumerable<string> cidrs) -> PrefixBlock
NextBlock(string cidr) -> PrefixBlock?
```

`PrefixBlock` fields (family-aware):

- `Family` (`InterNetwork` / `InterNetworkV6`)
- `Address` (the input host, if any)
- `Network`
- `PrefixLength`
- `SubnetMask` (IPv4 dotted; IPv6 null)
- `WildcardMask` (IPv4; IPv6 null)
- `Broadcast` (IPv4 only; IPv6 null)
- `FirstUsable` / `LastUsable` (IPv4 usable; IPv6 first/last in prefix)
- `TotalAddresses` (`BigInteger` — IPv6 `/0` is 2^128)
- `UsableHosts` (IPv4 rules; IPv6 = TotalAddresses)
- `IsHostRoute` (`/32` or `/128`)
- `IsPointToPoint` (IPv4 `/31` or IPv6 `/127`)
- `BinaryMask` (IPv4 32-bit string; IPv6 128-bit grouped)
- `TraditionalClass` (`A|B|C|D|E|None`)
- `Kind`
- `PtrHint` (IPv4 `/24` only, optional; otherwise null — do not invent classless in-addr)

`PrefixPlan`:

- `Parent`, `Rule`, `ChildPrefix`, `TotalNetworks`, `Networks` (≤ MaxList), `Unused`

`SubnetQuery`:

- `MaxList` default 1024
- `CountNetworkAndBroadcast` default false (IPv4)
- `PackLargestFirst` default true for VLSM

## 4. Math rules

### IPv4 hosts → prefix

Find smallest prefix `p` in 0..32 such that usable(p) ≥ N and the child fits in the parent.

| Prefix | Usable |
|---|---|
| 0–30 | 2^(32-p) − 2 |
| 31 | 2 |
| 32 | 1 |

200 hosts → `/24` (254).

### IPv4 networks → prefix

From parent `/P`, need K children → extra bits `ceil(log2(K))` → child `/P+bits` if ≤ 32.

25 networks from `/16` → 32 slots → `/21`.

### IPv6 hosts → prefix

No broadcast tax. Smallest `p` with `2^(128-p) ≥ N`. Typical LAN ask of 200 from a `/48` → `/121` mathematically; hosts that want SLAAC should pass `SplitPrefix(parent, 64)` rather than host count. Document that in the Demo.

### IPv6 networks → prefix

Same extra-bits rule. `/48` into ≥ 25 nets → `/53`.

### VLSM

IPv4: pack largest host-need first unless `PackLargestFirst = false`.  
IPv6: pack largest address-count first using the no-broadcast formula.  
Leftover listed in `Unused` (capped).

### Summarize

Smallest prefix that covers every input. If inputs are not a complete aligned set, still return the cover and set `Kind` note via plan rule `summarize-cover` (do not pretend they were contiguous allocations).

## 5. Failures

| Input | Result |
|---|---|
| Blank / unparsable | `ArgumentException` after HelperGuard |
| Prefix out of range | IPv4 0–32, IPv6 0–128 |
| Dotted mask on IPv6 | `ArgumentException` |
| Parent too small | `InvalidOperationException` after Reject |
| SplitPrefix child shorter than parent | Reject |
| MaxList < 1 | Reject |

## 6. Close gates (Phase 9)

- `ClassifyAddress("10.1.2.3").TraditionalClass == A` and Kind includes Rfc1918
- `ClassifyAddress("172.16.0.1")` → B + Rfc1918
- `ClassifyAddress("192.168.1.1")` → C + Rfc1918
- `ClassifyAddress("224.0.0.1")` → D + Multicast
- `ClassifyAddress("240.0.0.1")` → E
- `ClassifyAddress("2001:db8::1")` → None + Documentation
- Describe `192.168.10.37/24` → network 192.168.10.0, mask 255.255.255.0, broadcast 192.168.10.255, usable 1–254
- Prefix and mask agree on every IPv4 block
- PlanByHosts(`10.8.0.0/16`, 200) child prefix 24
- PlanByNetworks(`10.8.0.0/16`, 25) child prefix 21, TotalNetworks 32
- PackVlsm(`10.8.0.0/16`, [200,50,12,2]) first four as `/24` `/26` `/28` `/31`
- Describe `2001:db8::/32` First `2001:db8::` Last `2001:db8:ffff:ffff:ffff:ffff:ffff:ffff`, Broadcast null
- PlanByNetworks(`2001:db8::/48`, 25) child `/53`
- Contains / Overlaps true/false cases
- List of 2^20 children does not allocate 2^20 objects (MaxList)
- No cousin CLI spawn
