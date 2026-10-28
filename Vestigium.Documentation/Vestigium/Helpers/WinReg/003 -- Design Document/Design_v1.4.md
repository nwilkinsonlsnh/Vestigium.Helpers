# Vestigium.Helpers.WinReg — Design

**Document ID:** VEST-HLP-WINREG-DSN-000  
**Version:** 1.4  
**Status:** Locked companion to SRS v1.4  
**Date:** 21 September 2026  
**Binding:** `Requirements_v1.4.md` wins on conflict

This page records *why* WinReg is shaped this way. It does not add requirements.

---

## 1. Intent

One Windows façade over the registry. `RegistryHelper` is the static door. `RegistryClient` is Local or remote. Writes go through result + journal so inventory stays read-safe.

```
GetKey / GetValue / ListSubKeys / Search
Writes → confirm → RegistryWriteResult + vest-regjnl/1
WriteIndex → vest-regidx/1 (no payloads)
Compare → vest-regcmp/1
```

---

## 2. Locked decisions

| Decision | Why |
|---|---|
| Client, not static-only | Remote boxes share the same verbs. |
| Confirm on every write | Accidental SetValue must not land. |
| Result not exception on deny | Hosts can show a row. |
| Journal per line | Crash mid-batch still leaves readable history. |
| Index without payloads | Compare files can leave the box. |
| Never log value data | Secrets live in REG_SZ. |
| Never `Initialize` | Folder follows the host APPID. |

---

## 3. Shape

| File | Role |
|---|---|
| `RegistryHelper.cs` | Identity, Probe, Local/For, Export/Import/Search/Mount |
| `RegistryHelper.*.cs` | Compare, Apply, Journal, ACL, Hash |
| `RegistryClient.cs` | Get / List |
| `RegistryClient.Writes.cs` | Set / Delete / Rename |
| `RegistryComparer` / `RegistryIndexWriter` | Offline compare |
| `RegistryAcl` | DACL / owner |
| `WinRegCatalog` / `WinRegEvents` | Logging |

Tests live under `src/Vestigium.Helpers.Tests/`.

---

## 4. Exception / result policy

Missing keys return null. Writes return `RegistryWriteResult` (`Ok`, `Denied`, `NotFound`, `InUse`, `Unsupported`). `/` in a path throws. Compare cancel → Denied.

---

## 5. Still out

`reg.exe`, payload-in-index restore, remote WriteIndex (backlog B2).

---

## 6. Document control

| Version | Date | Change |
|---|---|---|
| 1.4 | (prior) | As-built stub. |
| 1.4 | 21 Sep 2026 | Expanded to standalone Design. |
