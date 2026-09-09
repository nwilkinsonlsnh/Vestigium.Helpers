# Vestigium.Helpers.FileIo — Phase Implementation Plan

**Document ID:** VEST-HLP-FILEIO-PLAN-000  
**Version:** 1.0  
**Status:** Active. Build mode follows this file.  
**Date:** 9 September 2026  
**Package:** `Vestigium.Helpers.FileIo`  
**Contract:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)

If this file and the SRS disagree, the SRS wins. If this file and working code disagree, change the code. Do not reopen locked decisions to make a slice easier.

Working tree of record: this repository (`Vestigium.Helpers`). Library root: `src/Vestigium.Helpers.FileIo/`.

---

## 0. How build mode uses this file

1. Read §1 (baseline) and §2 (locks) before touching code.
2. Implement **one phase**. Do not start the next phase until that phase's **Close gate** is green.
3. Commit at each Close gate. Message form: `FileIo phase N: <short goal>`.
4. Do not invent APIs that are not in SRS §7. Names may move a token; shapes may not.
5. Do not spawn `robocopy.exe`. Do not take admin rights. Do not log file contents.
6. Do not implement roadmap items in SRS §11 (LAD, scheduler, monitor, ACL, VSS).
7. After each phase run the commands in §9.

---

## 1. Baseline and repo actuals

Phase 0 is **paper + taxonomy**. It does not grow `FileIoHelper` past Identity and Probe **when starting from a skeleton**. This repository already shipped the engine on `main` before the SRS header was flipped to Accepted. Phase 0 still has to close: Status Accepted, this plan on disk, umbrella HLP-FIO filled, §8 subcategories registered.

| Phase | Close gate | On `main` as of 9 September 2026 |
|---|---|---|
| 0 Paper | SRS Accepted + taxonomy + this plan + umbrella HLP-FIO | Taxonomy registered (`bf73a6d`). SRS was still Proposed until the Phase 0 commit. |
| 1 Pure pieces | UniqueName, Compare, shred, prune tests | Shipped (`005ec0f`, `4350799`) |
| 2 Copy engine | Recon, buckets, Copy, progress | Shipped (`91c35a9`) |
| 3 Pause / Cancel / Audit / unique-content | Mid-file pause, cancel, Would*, SkipDuplicate | Shipped (`91c35a9`) |
| 4 Move / Delete / Mirror / demo | Purge on/off, seeded gallery | Shipped (`91c35a9`, `4aafae2`) |
| 5 Harden | Full SRS §10, retries, guide | **This phase.** Mid-file retry resume, `by=`/`reason=`, InUse/Unauthorized, injected index root, Developers Guide rewrite, §10 tests. |

Do not revert shipped engine code to satisfy a skeleton-era “do not grow the façade” note. Do not mix Encryption/Hashing dirty files into a FileIo commit.

---

## 2. Locked decisions (do not debate)

Copied from SRS §2. Build mode treats these as constants.

| # | Lock |
|---|---|
| 1 | `ReconLeadTime` default 15 s, allowed 0–180 inclusive. Above 180 throws `ArgumentOutOfRangeException`. No silent clamp. No 300 s. |
| 2 | `CertaintyPercent` is 100 only when `ReconComplete`. Eta is null until then. |
| 3 | Live `FileIoProgress` is chatty (250–500 ms). JSONL is sparse (SRS §6.2). |
| 4 | Product name is **Audit Mode**. |
| 5 | Delete uses the same recon team and the same five buckets. |
| 6 | LAD is not v1. |
| 7 | HelperLog only. Category `Helpers`. APPID `FileIo`. Library never calls `VestigiumLogger.Initialize`. |
| 8 | Default collision is `UniqueName`, pattern `.##`. Cap fails the item (`NameCap`). Never wrap into overwrite. |
| 9 | Pause finishes the current 64 KiB buffer and resumes `bytesCommitted`. Cancel aborts the current buffer and deletes dest this job created. |
| 10 | No scheduler, no admin/backup/VSS, no ACL copy. |
| 11 | Engine is BCL `FileStream` / `Directory` / `EnumerationOptions`. 64 KiB buffer. Never `ReadAllBytes` on a payload. |
| 12 | Visible paths and digest hex may be logged. File contents and `Exception` objects must not. |
| 13 | Five buckets, fixed: Tiny 0–256 KiB (8), Small 256 KiB–4 MiB (4), Medium 4–32 MiB (2), Large 32–256 MiB (1), Huge >256 MiB (1, only if Tiny+Small queued < 32). |
| 14 | Unique-content is digest identity. UniqueName is name identity. Both may be on. |
| 15 | Default retries 3 / 2 s. |
| 16 | Purge default false. Mirror without Purge leaves dest extras. |
| 17 | Probe is `%TEMP%` only. Tests never touch the real Desktop or live ProgramData. |

---

## 3. Phase 0 — Accept the paper and register taxonomy

**Goal.** Build mode and the repo agree on the contract.

**Edit**

- `src/Vestigium.Helpers.FileIo/_Documentation/Requirements_v1.0.md` — **Status: Accepted.**
- `src/Vestigium.Helpers.FileIo/_Documentation/DevelopersGuide_v1.0.md` — point at this plan.
- This file, in the same folder.
- `src/Vestigium.Helpers/HelperLog.cs` `CreateTaxonomy()` — register SRS §8:

  `Job`, `Recon`, `Copy`, `Move`, `Delete`, `Mirror`, `Index`, `Progress`, `Compare`, `Prune`, `SecureDelete`

  Keep `Probe`, `Identity`, `Guard`. (`Stats` is a later minor, not Phase 0.)
- Umbrella `_Documentation/Requirements_v1.0.md` HLP-FIO row: SRS v1.0 accepted.
- Lock the taxonomy with `FileIo_subcategories_are_registered` in `HelperLogTests`.

**Do not**

- Grow `FileIoHelper` in this commit if it is still Probe-only.
- Commit Encryption, Hashing, or Analytics handoff dirt with this phase.

**Close gate**

- Docs in `_Documentation/` are the accepted SRS + this plan + the guide.
- `dotnet test --filter FileIo_subcategories` (and existing suite tests still compile).
- Commit: `FileIo phase 0: accept SRS v1.0 and register taxonomy`.

---

## 4. Later phases (do not start from a skeleton if already shipped)

### Phase 1 — Pure pieces

Types, option guards, UniqueName (`.##` / `A##` + NameCap), `CompareFiles` via Hashing, SecureDelete, Prune. No `RunAsync`. Hashing must expose `HashFile` — do not put `Hash*` on FileIo.

### Phase 2 — Coordinator and Copy

Recon fills buckets. After lead time (or recon-complete), consumers copy. Live progress + sparse JSONL. Certainty may drop when recon finds more files.

### Phase 3 — Pause, Cancel, Audit Mode, unique-content

Pause parks at the next 64 KiB. Cancel aborts the buffer and deletes dest this job created. Audit Mode writes `Would*` and mutates nothing. Unique-content uses Hashing + dest index. Tests inject a temp index root.

### Phase 4 — Move, Delete, Mirror, demo

Move = copy then source delete per file. Delete uses the same buckets. Mirror + explicit Purge (default off). Seeded WPF gallery under APPID `FileIo`.

### Phase 5 — Harden

Retries 3 / 2 s, Unauthorized Failed-and-continue, no payload / no `EXCEPTION` in JSONL, Developers Guide matches the running engine, full SRS §10 green.

---

## 5. Logging cheat sheet

Category = `Helpers`. APPID = `FileIo`.

| Subcategory | When |
|---|---|
| Probe / Identity / Guard | Existing |
| Job | Recipe, pause, resume, cancel, final summary |
| Recon | Walk start, heartbeat, complete |
| Copy / Move / Delete / Mirror | Decisions for that verb |
| Index | Build, hit, clean |
| Progress | 15 s snapshot in JSONL only |
| Compare | Compare result |
| Prune | Empty-branch removal |
| SecureDelete | Pass count + path |

Never: file bytes, raw keys, PEM, `Exception` argument to HelperLog.

---

## 6. Explicitly out of this plan

LAD, directory monitor / trie, scheduler, admin / VSS / ACL, archive-bit flags, ADS, configurable bin edges, Analytics handoff (later SRS minor), encrypting the index, spawning `robocopy.exe`, `ReadAllBytes` on a payload.

---

## 7. Commands

From the repo root:

```
dotnet build src/Vestigium.Helpers.FileIo/Vestigium.Helpers.FileIo.csproj
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FullyQualifiedName~FileIo
dotnet test src/Vestigium.Helpers.Tests/Vestigium.Helpers.Tests.csproj --filter FileIo_subcategories
dotnet run --project src/Vestigium.Helpers.FileIo.Demo/Vestigium.Helpers.FileIo.Demo.csproj
```

---

## 8. Definition of done (v1.0)

1. SRS Status is Accepted and this plan is in `_Documentation/`.
2. HelperLog subcategories in SRS §8 are registered and tested.
3. SRS §7 surface exists. No robocopy process. No Hash/Seal APIs on FileIo.
4. Lead time 15 s default, rejected outside 0–180.
5. Certainty is 100 only after recon completion. Job and bucket progress both exist.
6. Audit Mode writes a full decision log and mutates nothing.
7. Default collision is UniqueName with cap-fail.
8. Cancel does not finish the current file. Pause resumes committed bytes.
9. No LAD, scheduler, or admin APIs.
10. SRS §10 tests pass. Demo can seed a Source/Dest pair and show Copy + Audit + buckets.

Phase 0 closes items 1–2. Items 3–10 are Phases 1–5.
