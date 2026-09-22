# Vestigium.Helpers.Services — Design

**Document ID:** VEST-HLP-SERVICES-DSN-000  
**Version:** 1.1  
**Status:** Locked companion to SRS v1.1  
**Date:** 21 September 2026  
**Binding:** `Requirements_v1.1.md` wins on conflict

This page records *why* Services is shaped this way. It does not add requirements.

---

## 1. Intent

One Windows façade over SCM. Snapshotter fills `ServiceInfo`. Control / logon / recovery are separate types so inventory never writes.

```
List / Get / Search(term)
Search(query) → KqlHelper.Compile(KqlPack.Service) → ServiceKqlRow
Start / Stop / SetLogon / SetRecovery → ServiceControlResult
Watch / Campaign sample on an interval
```

---

## 2. Locked decisions

| Decision | Why |
|---|---|
| Result not exception on Access Denied | Hosts can show a row without try/catch on every button. |
| ProtectedNames in one place | EventLog and peers must not change start type from a helper call. |
| Password never on `ServiceInfo` | Inventory must stay safe to serialize. |
| Tree cap + seen set | SCM graphs have cycles. |
| Campaigns in-process | Same pattern as Processes. |
| Never `Initialize` | Folder follows the host APPID. |

---

## 3. Shape

| File | Role |
|---|---|
| `ServiceHelper.cs` | Inventory, search, tree, control, watch |
| `ServiceHelper.Campaigns.cs` | Create / Load / remote client |
| `ServiceSnapshotter` | Fill `ServiceInfo` |
| `ServiceControl` / `ServiceLogon` / `ServiceRecovery` | Writes |
| `ServiceTreeWalker` | DependsOn / DependedBy |
| `ServiceKqlRow` | `IKqlRow` adapter |
| `ServiceCampaign` | Windows + JSONL |
| `ServicesCatalog` / `ServicesEvents` | Logging |

Tests live under `src/Vestigium.Helpers.Tests/`.

---

## 4. Exception policy

| Class | When |
|---|---|
| `ArgumentOutOfRangeException` | Bad interval or maxResults. |
| `ArgumentException` | Kql compile failed. |
| `InvalidOperationException` | Tree on a gone name. |
| `FileNotFoundException` | LoadCampaign missing recipe. |

Control APIs return `ServiceControlResult` instead of throwing on Access Denied.

---

## 5. Still out

systemd, `schtasks`, service install/delete, overriding ProtectedNames.

---

## 6. Document control

| Version | Date | Change |
|---|---|---|
| 1.1 | (prior) | As-built stub. |
| 1.1 | 21 Sep 2026 | Expanded to standalone Design. |
