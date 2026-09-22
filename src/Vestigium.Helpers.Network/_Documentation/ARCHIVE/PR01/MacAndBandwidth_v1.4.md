# Vestigium.Helpers.Network — MAC and bandwidth calculators (locked)

**Document ID:** VEST-HLP-NETWORK-SRS-MACBW-000  
**Version:** 1.4 addendum  
**Status:** Locked. Implements as Phase 10 (MAC/EUI) then Phase 11 (bandwidth).  
**Date:** 10 September 2026  
**Parent:** [`Requirements_v1.0.md`](Requirements_v1.0.md)

Unix time is **not** in this package.

---

## 1. Locks

| # | Lock |
|---|---|
| M1 | Pure math / explicit HTTP GET for OUI only. No cousin CLIs. |
| M2 | Bits vs bytes is an enum. Ambiguous `M` / `G` is a Reject. |
| M3 | Billing bases: `Day` = 86,400 s, `Days30` = 2,592,000 s (default), `Year365` = 31,536,000 s. Result always carries basis + seconds. |
| M4 | Website bots default **0**. Never invent a percentage. Catalog of common bots is a chooser, not a traffic model. |
| M5 | EUI-48 → modified EUI-64 **always** inserts `FF:FE` and **inverts the U/L bit**. Skipping the flip is a defect. |
| M6 | `ParseMac` / inventory never touch the network. |
| M7 | `LookupOui` is opt-in, timeout-bounded, fails typed when offline. Result includes disclaimer + source. |
| M8 | OUI name is the IEEE assignment at query time, not “current owner of this NIC.” |
| M9 | 95th-percentile billing is Phase 12. Uses Analytics P95 + these unit/basis types. No samples → no P95. |
| M10 | HelperLog subcategory `Address` (MAC) and `Bandwidth`. Summary only. |

---

## 2. Phase 10 — MAC / EUI

### Parse and format

Accept: colon, hyphen, Cisco `xxxx.xxxx.xxxx`, bare hex, `0x`, unsigned integer, EUI-64, IPv6 interface id.

Emit: colon (canonical uppercase), hyphen, Cisco, bare, integer, EUI-64, link-local.

Flags: multicast (I/G), locally administered (U/L), broadcast, unspecified.

### 48 → 64

1. Take the 48-bit value.
2. Insert `FF:FE` between OUI and NIC.
3. Invert bit 1 of the first octet (U/L).
4. That is modified EUI-64. Link-local = `fe80::` + that 64-bit id.

64 → 48 only when bytes 3–4 are `FF:FE`; invert U/L back. Otherwise throw.

### OUI lookup (opt-in)

```
NetworkHelper.ParseMac(text)                  // local only
NetworkHelper.LookupOui(mac, OuiLookupOptions)
```

`OuiLookupOptions`: `Timeout` default 3 s, `RegistryUrl` default a documented IEEE/MA-L endpoint or host-supplied URL. No lookup inside `GetAdapters` / Probe / Classify.

Result:

- `Vendor` string or null
- `Source` = `None | Live | File`
- `Disclaimer` = constant: assignment published for this prefix; not proof of current hardware owner; requires outbound HTTPS; may be stale or rate-limited.

Offline or timeout → `Vendor` null, `Source = None`, no throw from `ParseMac`. `LookupOui` itself may throw `TimeoutException` / `HttpRequestException` after HelperGuard.

Hosts that must stay offline never call `LookupOui`.

OUI file snapshot (offline registry) is allowed later as `Source = File`. Not required for Phase 10.

---

## 3. Phase 11 — Bandwidth

### Units

Enum: `Bit`, `Byte`, `Kb`, `KB`, `Kib`, `KiB`, `Mb`, `MB`, `Mib`, `MiB`, `Gb`, `GB`, `Gib`, `GiB`, `Tb`, `TB`, `Tib`, `TiB`.

`b` = bit, `B` = byte. SI = 1000, IEC = 1024.

### Bases

`BandwidthBasis.Day | Days30 | Year365`.
Default for monthly/hosting convert: `Days30`.

Per-day and per-year are first-class converts from the same amount, not separate math.

### Modes

- Convert size ↔ size (always show bit and byte views).
- Transfer time: size + rate → duration; size + duration → rate; rate + duration → size.
- Rate ↔ volume for Day / Days30 / Year365.
- Website estimate: page size × (human hits + selected bot hits) × asset factor × origin ratio × days in basis.

### Bot catalog (chooser)

Ids only. Hits stay operator-entered. Default all bot hits = 0.

| Id | Display |
|---|---|
| Googlebot | Google |
| GooglebotImage | Google Images |
| GooglebotVideo | Google Video |
| AdsBotGoogle | Google Ads |
| Bingbot | Microsoft Bing |
| DuckDuckBot | DuckDuckGo |
| Applebot | Apple |
| Amazonbot | Amazon |
| YandexBot | Yandex |
| Baiduspider | Baidu |
| Slurp | Yahoo |
| FacebookBot | Meta |
| LinkedInBot | LinkedIn |
| TwitterBot | X / Twitter |
| AhrefsBot | Ahrefs |
| SemrushBot | Semrush |
| DotBot | Moz |
| PetalBot | Huawei Petal |
| GPTBot | OpenAI |
| ChatGPTUser | OpenAI user-initiated |
| ClaudeBot | Anthropic |
| Bytespider | ByteDance |
| Other | Operator-named |

Catalog is data on `NetworkHelper.CommonBots`. It does not assign megabytes.

### Website query

- `PageSize`
- `HumanHits`
- `BotRows` = list of `{ BotId, Hits }` (omit or 0 = none)
- `AssetFactor` default 1
- `OriginRatio` default 1
- `PeakFactor` default 1
- `Basis` default Days30

Result: total hits, bot hits, monthly/daily/yearly bytes, average rate, peak rate if factor ≠ 1, basis seconds.

---

## 4. Phase 12 (later) — P95 + offline OUI file

- Accept samples (bits/s) or Analytics `NumericSeries`.
- P95 via Analytics. Network only converts that number into units + Day/Days30/Year365.
- Optional packed OUI snapshot for air-gapped hosts.

---

## 5. Close gates

**Phase 10**

- Parse Cisco `001A.2B3C.4D5E` → colon `00:1A:2B:3C:4D:5E`
- Integer round-trip for a known unicast MAC
- Broadcast and multicast flags
- `00:1A:2B:3C:4D:5E` → modified EUI-64 has `FF:FE` and first octet U/L flipped vs input
- Link-local starts with `fe80:`
- EUI-64 without `FF:FE` in the middle → throw on ToEui48
- `ParseMac` works with the NIC unplugged (no HTTP)
- `LookupOui` with a dead endpoint / blocked network → typed fail or null vendor + Source None, no hang beyond Timeout

**Phase 11**

- `10 MB` ≠ `10 Mb`
- 20 Mbps × Days30 = labeled byte volume using 2,592,000 s
- Same amount exposes Day and Year365 views
- Website with bots omitted → bot hits 0
- Website with Googlebot hits 10,000 adds those hits only
- Rate 0 + nonzero size → throw
