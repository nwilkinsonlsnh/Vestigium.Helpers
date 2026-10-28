# Vestigium.Helpers.Services — Requirements

**Document ID:** VEST-HLP-SERVICES-SRS-000  
**Version:** 1.1  
**Status:** Current. Incorporates the v1.1 addendum.  
**Date:** 21 September 2026  
**Package:** `Vestigium.Helpers.Services`  
**TFM:** `net10.0-windows`

Companion addendum: [`Requirements_v1.1_Addendum.md`](Requirements_v1.1_Addendum.md).

---

## 1. Purpose

Give a Windows host one façade (`ServiceHelper`) over the Service Control Manager: list, search, tree, start/stop, logon, recovery, watch, campaigns.

---

## 2. Locked decisions

| # | Decision |
|---|---|
| 1 | Windows only. |
| 2 | Missing name: `Get` / `TryGet` return null / false. |
| 3 | Visible / Hidden / All are disjoint or union as named. `ListHidden` is disjoint from Visible. |
| 4 | Term search and Kql search. Cap 256. |
| 5 | Tree cap 256 unique names. Cycles stop that branch (`AmbiguousDependency`). |
| 6 | Control returns `ServiceControlResult`. Access Denied is not an exception. |
| 7 | `ProtectedNames` refuse Stop, Restart, StartType, Logon, Recovery. Confirm does not override. |
| 8 | Never persist or log the account password. |
| 9 | Watch interval 250 ms through 60 s. |
| 10 | Campaigns in-process. No `schtasks`. |
| 11 | Library never calls `VestigiumLogger.Initialize`. APPID `Services`. |

---

## 3. Goals

**G1.** Inventory Win32 and driver services, visible and hidden.  
**G2.** Kql filter on `KqlPack.Service`.  
**G3.** Dependency tree without unbounded walk.  
**G4.** Control and config with fail-closed protected names.  
**G5.** Campaign JSONL samples while a window is open.

---

## 4. Still out

Linux systemd, scheduled-task campaigns, deleting services, changing protected core services.
