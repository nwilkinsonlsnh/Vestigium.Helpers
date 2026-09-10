# File share transfer campaigns (locked)

**Document ID:** VEST-HLP-NETWORK-SRS-SHARE-000  
**Version:** 1.5 addendum  
**Status:** Locked. FileIo 13a → Network 13b.  
**Date:** 10 September 2026

Parent locks from the prior recommendation stand: Network does not copy, FileIo copies, no `net use` / `mount`, no cousin CLIs, credentials never in JSONL, efficiency default 1.0, declared-pipe estimate stays labeled separately from measured.

---

## 1. Two modes

| Mode | When | What it measures |
|---|---|---|
| **Default** | Operator has a share and a planned size (or “1 TB”) and does not want a source crawl | One sequential probe: 64 MiB × 4 writes (override size/count). P95 bytes/s → `TransferTime(plannedSize)`. |
| **Advanced** | Operator also has a **source directory** | FileIo analyzes that tree (sizes only). Analytics describes the mix. Network builds a **probe recipe from the mix**, measures each bucket on the share, then scales **per bucket**. |

Default never invents a workload. Advanced never invents rates — it only chooses probe *shapes* from source statistics.

---

## 2. FileIo addition (13a)

Copy/Mirror already recon a tree into Tiny / Small / Medium / Large / Huge and, after a real transfer, emit `FileIoJobStats` (`FileSizes`, `TransferRates`, per-bucket series).

What FileIo still needs as a named API (Audit Mode on Copy is not enough for hosts):

```
FileIoHelper.AnalyzeDirectory(path, FileIoAnalyzeOptions?)
  → FileIoDirectoryAnalysis
```

- Recurse the path. Do **not** copy.
- Record every file size. No `ReadAllBytes`.
- Build the same five buckets + `FileIoSeriesSnapshot` via Analytics (min, Q1, median, Q3, P95, mean, count, high outliers).
- Also return `FileCount`, `TotalBytes`, `DirectoryCount`.
- Cap / exclude same as other FileIo jobs (hidden, system — follow existing options).
- Empty directory → counts 0, empty snapshots (not a throw).
- Missing path → typed fail.

Second FileIo API for the campaign itself:

```
FileIoHelper.WriteProbe(directory, byteCount, FileIoProbeOptions?)
FileIoHelper.ReadProbe(path)
```

- Write a throwaway file of exact length (random or zero — default random so NAS compression does not lie).
- Return bytes, duration, bytes/sec using the same observation type as a job.
- Delete after unless `KeepProbe = true`.
- Never used as a general copy API.

---

## 3. How Advanced picks probes

Planner input: `FileIoDirectoryAnalysis` of the **source** (not the share).

Planner output: `ShareProbePlan`.

Rules:

1. `PlannedSize` = analysis `TotalBytes` unless the operator overrides (“we will send 1 TB even if this folder is 200 GB”).
2. For each FileIo bucket with `FileCount > 0` **and** `TotalBytes` above a floor (default 1 MiB of that bucket):
   - Probe size = clamp(bucket median, 4 KiB, 64 MiB). Median, not mean — huge outliers must not pick a 8 GiB probe.
   - Probe count = 2 if the bucket holds ≥ 20% of total **bytes**, else 1.
3. If Tiny+Small **file count** is ≥ 80% of files **and** those buckets are ≤ 20% of bytes, add one **metadata probe**: 256 files × 4 KiB. Label it `Workload = ManySmall`. This is the case where a 64 MiB probe would lie.
4. If only Huge/Large matter (videos), skip the metadata probe. Default 64 MiB sequential is close enough; Advanced still runs the Large/Huge median probe so measured rate is on-share.
5. Hard cap: total probe bytes ≤ 256 MiB unless the operator raises `MaxProbeBytes`.

Network does not invent a sixth bucket. It reuses FileIo’s five.

---

## 4. How Advanced estimates time

After probes run on the share:

```
for each bucket with source bytes:
  rate = P95 of that bucket's probe rates (or Default rate if that bucket was not probed)
  payloadHours += sourceBytesInBucket / rate / efficiency

metadataHours += metadataProbe.SecondsPerFile * sourceFileCountInTinySmall   // only if metadata probe ran

MeasuredDuration = payloadHours + metadataHours
```

Report both parts. Operators need to see “payload 3.1 h + metadata 1.4 h.”

Declared-pipe estimate (optional link rate) stays a third number, never mixed in.

---

## 5. Default mode (no source tree)

- Probe: 64 MiB × 4, one stream, write (read optional).
- Planned size: operator-supplied (`1 TiB` via `DataUnit`).
- Scale: `TransferTime(plannedSize, P95 rate)`.
- Disclaimer: linear scale from 64 MiB sequential; many-small-file trees will be slower — use Advanced.

---

## 6. Ownership

| Type | Home |
|---|---|
| `AnalyzeDirectory`, `WriteProbe`, `ReadProbe`, buckets, size series | FileIo |
| `FileShareTarget`, campaign, JSONL, planner, `TransferTime` scale-up | Network |
| Percentile / five-number on sizes and rates | Analytics (already called by FileIo) |

Network may reference FileIo for the planner input type and to *invoke* probe writes. It must not open `FileStream` itself.

---

## 7. Close gates

**FileIo AnalyzeDirectory**

- Folder of 10 × 1 KiB + 1 × 10 MiB → Tiny count 10, Large/Medium has the 10 MiB, `TotalBytes` exact, no files copied.
- Missing path throws.

**FileIo WriteProbe**

- 1 MiB probe to temp → file gone after return (default), observation has bytes/sec > 0.

**Network Default**

- Planned 1 GiB + four identical probe rates → duration = 1 GiB / P95.

**Network Advanced**

- Analysis with 90% of files Tiny and 90% of bytes Huge → plan includes metadata probe **and** a Huge/Large probe.
- Analysis with only 20 × 80 MiB files → no metadata probe, Large/Huge probe only.
- Estimate line shows payload hours and metadata hours separately when both ran.
