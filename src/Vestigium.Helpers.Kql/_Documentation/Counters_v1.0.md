# Vestigium.Helpers.Kql — Counter catalog notes

**Status:** Draft addendum to [`Requirements_v1.0.md`](Requirements_v1.0.md) §3.  
**Date:** 10 September 2026

Stay inside the groups already named: `PROC`, `CPU`, `GPU`, `MEM`/`RAM`, `DISK`, `IO`, `NET`. Do not invent `TEMP`, `BATTERY`, or `SECURITY` for v1.

Row type matters. A process query must not see `MEM.CommitLimit`. A system query must not see `PROC.CommandLine` unless the host joined them.

---

## Add in v1 (you already collect most of these)

### PROC

| Field | Why |
|---|---|
| `PROC.Threads` | `Name LIKE '%edge%' && PROC.Threads GT 80` |
| `PROC.Handles` | leak hunts |
| `PROC.StartTime` | campaign identity (pid + startTime + name) |
| `PROC.Company` | `Company LIKE 'Microsoft%'` |
| `PROC.ImageType` | x86 vs x64 |
| `PROC.Integrity` | High / System / Low |
| `PROC.WindowTitle` | already a search field |

Leave signer / CFG / DEP / ASLR / Package / Autostart on the full row. They are columns, not campaign filters, until someone writes those queries daily.

### CPU

| Process | System |
|---|---|
| `CPU.UserTime` `CPU.KernelTime` | `CPU.Usage` |
| `CPU.TimeDelta` | `CPU.ContextSwitchDelta` |
| `CPU.Priority` | `CPU.InterruptDelta` `CPU.DpcDelta` |
| | `CPU.Cores` `CPU.Sockets` `CPU.LogicalProcessors` |

### MEM / RAM

| Process | System |
|---|---|
| `MEM.VirtualBytes` | `MEM.PhysicalTotal` `MEM.PhysicalAvailable` `MEM.PhysicalPercent` |
| `MEM.WorkingSetPeak` | `MEM.CommitCurrent` `MEM.CommitLimit` `MEM.CommitPeak` `MEM.CommitPercent` |
| `MEM.PageFaults` `MEM.PageFaultDelta` | `MEM.CommitChange` |
| `MEM.PrivateBytesDelta` `MEM.WorkingSetDelta` | `MEM.CacheWS` `MEM.KernelWS` `MEM.DriverWS` |
| | `MEM.Paged` `MEM.Nonpaged` `MEM.PagedLimit` `MEM.NonpagedLimit` |

System paging lists: ship **`MEM.Zeroed` `MEM.Free` `MEM.Modified` `MEM.Standby`** only. Skip Priority 0–7 in v1 — they are a System Informer pane, not a filter language.

### GPU

Keep Usage / Dedicated / System / Committed. Add:

| Field | Notes |
|---|---|
| `GPU.Adapter` | name; Adapter pack |
| `GPU.Engine` | 3D / Compute / Copy when the instance string is available |

Do **not** add clocks, power, temperature, fan in v1. Those need vendor APIs, not the PDH GPU Engine counters you already use.

### IO

Add the third bucket Windows already has (`GetProcessIoCounters` / system performance):

| Field |
|---|
| `IO.Other` `IO.OtherBytes` |
| `IO.OtherDelta` `IO.OtherBytesDelta` |
| `IO.ReadBytesDelta` `IO.WriteBytesDelta` |
| `IO.BytesPerSec` |

### DISK (system pack)

You already snapshot these as paging counters. Put them on DISK, not MEM:

| Field |
|---|
| `DISK.PageReadDelta` |
| `DISK.PagingFileWriteDelta` |
| `DISK.MappedFileWriteDelta` |

Per-process disk bytes stay on `IO.*`. Volume free-space is a different entity (`Volume`); not v1.

### NET (system / adapter only in v1)

Per-process network on Windows is ETW. Do not pretend `NET.BytesSent` exists on a `Process` row in v1.

| Field | Pack |
|---|---|
| `NET.BytesSentDelta` `NET.BytesRecvDelta` | System or Adapter |
| `NET.Connections` | System |

---

## Do not add yet

- Paging list Priority 0–7 and PagedFileModified as first-class KQL names
- GPU temperature / power / clocks
- Disk queue length, split I/O, per-volume capacity
- Per-process NET
- Battery, thermal zones, audio
- Every mitigation flag as a filter token

Those can attach later as catalog rows without a parser change.
