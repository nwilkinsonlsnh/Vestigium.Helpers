# Vestigium.Helpers.Services — Requirements Specification

**Document ID:** VEST-HLP-SERVICES-SRS-000  
**Version:** 1.0  
**Status:** Draft — first lossless revision. Replaces the 7 September 2026 skeleton.  
**Date:** 11 September 2026  
**Package:** `Vestigium.Helpers.Services`  
**Project ID:** HLP-SVC  
**TFM:** `net10.0-windows` (.NET 10 LTS) — **Windows-only in v1**  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)  
**Cousins:** [`Vestigium.Helpers.Processes`](../../Vestigium.Helpers.Processes/_Documentation/Requirements_v1.0.md), [`Vestigium.Helpers.Kql`](../../Vestigium.Helpers.Kql/_Documentation/Requirements_v1.0.md)  
**Hosts:** later Vestigium solution, WPF gallery `Vestigium.Helpers.Services.Demo` (not expanded in this milestone)

If implementation and this file disagree, this file wins.

This package is a **library of resources**, not services.msc and not a shell. Hosts subscribe. It reads the Service Control Manager (SCM) and publishes typed snapshots. It does not spawn `sc.exe`, `net.exe`, `powershell.exe`, `services.msc`, or System Informer.

This is not Processes. Processes talks to the OS process table. This talks to SCM. When a service is running and has a PID, Services may **join** a `ProcessInfo` from `ProcessHelper.Get(pid)`. It does not re-implement CPU / GPU / I/O counters.

This is not ASP.NET `IHostedService`. That surface is out of v1.

---

## 0. How to read this document

It records:

- one façade (`ServiceHelper`) for identity, Probe, list, get, search, dependency walk, watch, start, stop, pause, continue, restart
- one immutable **service snapshot** (`ServiceInfo`)
- one **dependency tree** (depends-on / depended-by), not a process parent/child tree
- one **watcher** for state and, when a PID is present, joined process resource samples
- search: `StartsWith` / `EndsWith` / `Contains` plus KQL against `KqlPack.Service`
- campaigns: named recipes + wall-clock windows that append JSONL while the host stays running
- HelperLog only; Category `Helpers`; APPID `Services`
- partial data: unavailable fields are `null` plus an availability flag. Never invent a PID, a start type, or a failure action

services.msc and System Informer’s Services tab are the *column reference*. They are not spawned and they are not a dependency.

---

## 1. Purpose

Give every Vestigium host one way to:

1. List every Win32 service and (opt-in) kernel / file-system driver the token can see.
2. Describe a service the way an operator already reads services.msc.
3. Watch status and the hosted process without polling by hand.
4. Walk depends-on and depended-by.
5. Start, stop, pause, continue, and restart with a typed result and an audit line.
6. Filter with StartsWith / EndsWith / Contains or with KQL (`SVC.*`).
7. Leave a campaign running so counters land in JSONL on a schedule.

Typical hosts: a gallery that looks like a service table; ProbeHost checking whether a helper service is still Running; an operator restarting a stuck share-process service without opening services.msc.

```csharp
var snap = ServiceHelper.Get("Spooler");
var tree = ServiceHelper.GetDependencyTree("LanmanServer");
using var watch = ServiceHelper.Watch("Spooler", TimeSpan.FromSeconds(1), ServiceWatchFields.Status | ServiceWatchFields.Process);
watch.Sampled += (_, s) => ui.Render(s);

var hits = ServiceHelper.Search("sql", ServiceSearchMode.Contains);
var kql  = ServiceHelper.Search("Status == 'Running' && StartType == 'Automatic'");
```

---

## 2. Architectural constraints (binding)

| ID | Constraint |
|---|---|
| A1 | Class library `net10.0-windows`. No WPF, no Themes, no Controls. The Demo gallery is a separate host and is **not** in scope for this SRS revision. |
| A2 | Independently referenced. Core `Vestigium.Helpers` for guards and `HelperLog` only. `Vestigium.Helpers.Kql` for compile / match. `Vestigium.Helpers.Processes` is an **optional join** when a running service has a PID; Services must still list Stopped services if Processes is absent at compile time — v1 **does** reference Processes so the join is first-class. WinReg / Hashing / Encryption / Analytics / Charts are not referenced in v1. |
| A3 | The library never calls `VestigiumLogger.Initialize`. It never chooses a log folder. Boundary calls write Debug enter and Information listed/started/stopped; rejects write Error then throw. Inner watcher ticks do **not** write JSONL (campaigns do, on purpose). |
| A4 | Public work lives on `ServiceHelper` plus immutable snapshots. SCM handles and P/Invoke stay internal. `System.ServiceProcess.ServiceController` is an implementation detail, not the public contract. |
| A5 | A `ServiceInfo` is a **snapshot**, not a live controller leaked to the caller. Hosts that want change over time use `IServiceWatcher`. |
| A6 | No tool spawn. No `sc start`. No WMI `Win32_Service.StartService` as the control path. |
| A7 | Privileged fields the current token cannot read are `null` with `Availability = Denied` or `Unsupported`. `List` / `Get` do not throw because one service refused `SERVICE_QUERY_CONFIG`. Throw only on contract violations (null name, interval out of range, empty campaign id). |
| A8 | Control verbs are explicit. There is no “stop on dispose” and no implicit cascade-stop of dependents. Cascade stop / start is a separate method with `Confirm = true`. |
| A9 | Credentials and service account passwords supplied to config changes are never logged, never written to JSONL, never kept after the SCM call returns. |
| A10 | Tests must not stop `EventLog`, `RpcSs`, `DcomLaunch`, `LSM`, `PlugPlay`, `ProfSvc`, `SamSs`, `Schedule`, `Winmgmt`, `CryptSvc`, or the SCM itself. Tests use a disposable service installed under a unique name in `%TEMP%` or operate read-only against well-known services (`EventLog` query only). |
| A11 | Linux / macOS / systemd are **out of v1**. The umbrella SRS currently lists this project as `net10.0`; v1.0 changes that to `net10.0-windows`. |
| A12 | Create-service and Delete-service are **out of v1**. Changing binary path is out of v1. Changing start type is in v1 with `Confirm = true`. |

---

## 3. Decisions locked in this version

| # | Decision | Locked as |
|---|---|---|
| 1 | TFM | **`net10.0-windows`**. The field set below is SCM. |
| 2 | Cousin tools | services.msc / System Informer Services tab are the **column reference**. Never spawned. |
| 3 | Two tempos | **Snapshot** and **Watcher**. Watchers exist for Status, Pid, Checkpoint / WaitHint, and joined process resource fields. Identity and config fields are not watched. |
| 4 | Watcher interval | Default **1 second**. Allowed **250 ms – 60 s**. Outside that range → `ArgumentOutOfRangeException`. No silent clamp. |
| 5 | Process join | When `Pid` is set, `ServiceInfo.Process` may hold a slim `ProcessInfo` from `ProcessHelper.Get(pid, Slim)`. Shared-process services (`svchost`) share one PID; the library does **not** pretend the CPU% belongs only to this service. The row records `SharedProcess = true` when `ServiceType` is share-process. |
| 6 | Units | Joined process memory is **bytes** (`long`), same as Processes. |
| 7 | Partial rows | `List()` never fails the whole table because one service refused query-config. That row is present with Name + Status and the rest null / Denied. |
| 8 | Search | `StartsWith`, `EndsWith`, `Contains`. Case-insensitive ordinal. Default fields: Name, DisplayName, Description, ImagePath. KQL overload uses `KqlPack.Service`. |
| 9 | Tree | Edges are SCM dependencies, not PPIDs. Cycle guard marks `AmbiguousDependency` and stops that branch. |
| 10 | Control | Start / Stop / Pause / Continue / Restart return `ServiceControlResult`. They do not throw on Access Denied or Dependent Services Running — those are result codes. |
| 11 | Protected / essential | Control of a service the token cannot open for the required access returns `Denied`. The library does not disable Critical Process or patch SCM. |
| 12 | Drivers | `List` default is **Win32 services only**. `ServiceKind.Drivers` or `ServiceKind.All` is opt-in. Kernel drivers that are not stoppable stay queryable. |
| 13 | Comment | Host annotation keyed by canonical service name. Stored under `%ProgramData%\Vestigium\Services\Comments.json` only when the host asks to persist. Default is in-memory. |
| 14 | Logging door | `HelperLog` only. Category `Helpers`. APPID `Services`. Sparse: list/search start+count, start, stop, pause, continue, restart, watcher start/stop, campaign start/stop. No per-tick sample lines except campaign JSONL. |
| 15 | Campaigns | Same shape as Processes: named recipe, name/KQL filter, wall-clock windows (`TimeOnly` + duration), JSONL append. Target is matching **services**, not PIDs. |
| 16 | Probe | On-box only. Lists the current machine’s service count and fetches `EventLog` (query). Does not start or stop anything. |
| 17 | KQL pack | Hosts call `KqlHelper.Create(KqlPack.Service)`. Services binds `ServiceKqlRow`. Expanding `SVC.*` beyond Name / DisplayName / Status / StartType / Pid is in scope for this library’s implementation plan (catalog change lives in Kql). |

---

## 4. Glossary (binding language)

| Term | Meaning |
|---|---|
| **Snapshot** | Immutable values captured at one instant. |
| **Watcher** | An `IDisposable` that samples selected fields every `Interval` and raises `Sampled`. |
| **Service name** | SCM key name (`Spooler`), not the display name. Stable key for Get / Control. |
| **Display name** | Localized operator name (`Print Spooler`). |
| **Status** | SCM current state: Stopped / StartPending / StopPending / Running / ContinuePending / PausePending / Paused. |
| **Start type** | Boot / System / Automatic / AutomaticDelayed / Manual / Disabled / Trigger. |
| **Image path** | `lpBinaryPathName` from query-config, including arguments as SCM stored them. |
| **PID** | Hosted process id when Status is Running (or pending) and SCM reports one. Null when Stopped. |
| **Shared process** | More than one service hosted in the same process. |
| **Depends-on** | Services this service lists as dependencies. |
| **Depended-by** | Services that list this service as a dependency. |
| **Availability** | `Available`, `Denied`, `Unsupported`, `Gone`. |
| **Gone** | Named service existed at request and was deleted before the field could be read. |
| **Search mode** | `StartsWith`, `EndsWith`, `Contains`. |
| **Control** | Start / Stop / Pause / Continue / Restart against SCM. |
| **Campaign** | Named recipe + windows that append JSONL while the host process stays alive. |

These words are not interchangeable. Host UI copy and XML docs must use them as defined here.

---

## 5. Goals

**G1.** One façade (`ServiceHelper`) owns Identity, Probe, list, get, search, tree, watch, start, stop, pause, continue, restart, change start-type, campaigns, and comments.  
**G2.** List every service the token can see, including ones that only allow limited query.  
**G3.** `ServiceInfo` carries the v1 field set in §6.  
**G4.** Watchers exist for Status, Pid, controls accepted / wait hint, and joined process resources.  
**G5.** Search is StartsWith / EndsWith / Contains across a documented field mask, plus a KQL string overload.  
**G6.** Dependency walk returns depends-on, depended-by, and a nested tree with a cycle guard.  
**G7.** Control verbs return a typed result per service name. They do not throw on Access Denied.  
**G8.** Campaigns write JSONL for matching services inside wall-clock windows.  
**G9.** Logs meet ALCOA+ through HelperLog. Passwords never appear.  
**G10.** `Probe` is read-only.

---

## 6. Service snapshot (`ServiceInfo`)

Every field below is in scope for v1. Groups exist so a host can request a **slim** list (identity + status) or a **full** row (config + failure + triggers + join).

`List()` default is slim. `Get(name)` default is full.

### 6.1 Identity

| Field | Type | Notes |
|---|---|---|
| `Name` | `string` | SCM key. Required. Always present on a returned row. |
| `DisplayName` | `string?` | |
| `Description` | `string?` | Service description from SCM / registry. |
| `Kind` | `ServiceKind` | `Win32` / `Driver` / `FileSystemDriver` / `RecognizerDriver` / `Unknown`. |
| `ServiceType` | `ServiceTypeFlags` | Own process / share process / interactive / kernel / adapter bits as reported. |
| `SharedProcess` | `bool` | True when share-process bit is set. |
| `Comment` | `string?` | Host annotation (§3.13). |

### 6.2 State

| Field | Type | Watchable | Notes |
|---|---|---|---|
| `Status` | `ServiceStatus` | Yes | See glossary. |
| `Pid` | `int?` | Yes | Null when not running. |
| `Win32ExitCode` | `int?` | — | |
| `ServiceSpecificExitCode` | `int?` | — | |
| `Checkpoint` | `int?` | Yes | Pending controls. |
| `WaitHint` | `TimeSpan?` | Yes | |
| `ControlsAccepted` | `ServiceControls` | — | Stop / PauseContinue / Shutdown / ParamChange / NetBind / HardwareProfile / SessionChange / Preshutdown / … |
| `Process` | `ProcessInfo?` | Yes | Slim join when Pid is set and Processes is referenced. |

### 6.3 Configuration

| Field | Type | Notes |
|---|---|---|
| `StartType` | `ServiceStartType` | Boot / System / Automatic / AutomaticDelayed / Manual / Disabled. |
| `DelayedAutoStart` | `bool?` | Distinct from StartType when query-config2 is available. |
| `TriggerStart` | `bool?` | True when one or more SCM triggers exist. |
| `ErrorControl` | `ServiceErrorControl?` | Ignore / Normal / Severe / Critical. |
| `ImagePath` | `string?` | Binary + arguments as stored by SCM. |
| `LoadOrderGroup` | `string?` | |
| `TagId` | `int?` | |
| `Account` | `string?` | `LocalSystem` / `NT AUTHORITY\LocalService` / `NT AUTHORITY\NetworkService` / domain account / virtual service account. Password never returned. |
| `DesktopInteract` | `bool?` | SERVICE_INTERACTIVE_PROCESS. |
| `SidType` | `ServiceSidType?` | None / Unrestricted / Restricted. |
| `RequiredPrivileges` | `IReadOnlyList<string>?` | |
| `LaunchProtected` | `ServiceLaunchProtected?` | None / Windows / WindowsLight / AntimalwareLight when queryable. |
| `PreshutdownTimeout` | `TimeSpan?` | |

### 6.4 Dependencies

| Field | Type | Notes |
|---|---|---|
| `DependsOn` | `IReadOnlyList<string>` | Service names this one needs. Empty list, not null, when none. |
| `DependedBy` | `IReadOnlyList<string>` | Names that need this one. Computed at snapshot time. |
| `AmbiguousDependency` | `bool` | Cycle or missing name during walk. |

### 6.5 Failure actions and triggers

| Field | Type | Notes |
|---|---|---|
| `FailureResetPeriod` | `TimeSpan?` | |
| `FailureRebootMessage` | `string?` | |
| `FailureCommand` | `string?` | Command SCM would run. Never executed by this library. |
| `FailureActions` | `IReadOnlyList<ServiceFailureAction>` | Run / Restart / Reboot / None + delay. |
| `Triggers` | `IReadOnlyList<ServiceTriggerInfo>` | Type / action / subtype when query-config2 is available. Missing API → `Unsupported`, not empty-pretend. |

### 6.6 Availability

```text
readonly record struct FieldAvailability(ServiceField Field, Availability State, string? Reason);
```

`ServiceInfo.Availability` is the list of fields that are not `Available`. A host can show a lock icon without guessing.

### 6.7 Slim vs full

| Level | Includes |
|---|---|
| `Identity` | Name, DisplayName, Kind, Status, Pid |
| `Slim` | Identity + StartType + ImagePath + SharedProcess |
| `Full` | Everything in §6 |

Default `List` = Slim. Default `Get` / detail search = Full.

---

## 7. Dependency tree

```text
ServiceTree GetDependencyTree(string name, ServiceTreeDirection direction = DependsOn)
IReadOnlyList<ServiceInfo> GetDependsOn(string name)
IReadOnlyList<ServiceInfo> GetDependedBy(string name)
```

```text
enum ServiceTreeDirection { DependsOn, DependedBy, Both }

sealed class ServiceTree
{
    ServiceInfo Root { get; }
    IReadOnlyList<ServiceTree> Children { get; }
    IReadOnlyList<ServiceInfo> Flatten();   // depth-first, includes root
}
```

Cycle guard: if a dependency chain loops, stop that branch and mark `AmbiguousDependency`.

This is **not** a process tree. A running service’s hosted process tree is `ProcessHelper.GetTree(pid)` and is the host’s composition, not a Services method in v1.

---

## 8. Lifetime operations

### 8.1 List and get

```text
IReadOnlyList<ServiceInfo> List(
    ServiceDetailLevel level = Slim,
    ServiceKind kind = Win32)

ServiceInfo? Get(string name, ServiceDetailLevel level = Full)
bool TryGet(string name, out ServiceInfo info)
```

`Get` returns null when the name does not exist. It does not throw for Gone. Names are compared case-insensitive (SCM rule).

### 8.2 Search

```text
IReadOnlyList<ServiceInfo> Search(
    string term,
    ServiceSearchMode mode,
    ServiceSearchFields fields = Name | DisplayName | Description | ImagePath,
    ServiceDetailLevel level = Slim,
    int maxResults = 256)

IReadOnlyList<ServiceInfo> Search(
    string query,                    // KQL, pack = Service
    ServiceDetailLevel level = Slim,
    int maxResults = 256)
```

Max results cap is 256. Values above that are rejected (`ArgumentException`), not silently clamped.

KQL uses three-valued logic from Kql. Missing Description does **not** match `Description == ''`. `==` treats `% * ?` as literals and Kql logs a Warning when those characters appear on `==`.

### 8.3 Watch

```text
IServiceWatcher Watch(
    string name,
    TimeSpan interval,
    ServiceWatchFields fields = Status | Pid)

IServiceWatcher Watch(
    string query,                    // KQL over the live list
    TimeSpan interval,
    ServiceWatchFields fields = Status | Pid)
```

First tick may have joined-process percent / delta = null (same rule as Processes). Status is always the current SCM state.

### 8.4 Control

```text
ServiceControlResult Start(string name, IReadOnlyList<string>? arguments = null, TimeSpan? timeout = null)
ServiceControlResult Stop(string name, TimeSpan? timeout = null, bool confirmDependents = false)
ServiceControlResult Pause(string name, TimeSpan? timeout = null)
ServiceControlResult Continue(string name, TimeSpan? timeout = null)
ServiceControlResult Restart(string name, TimeSpan? timeout = null, bool confirmDependents = false)

IReadOnlyList<ServiceControlResult> StopSearch(string queryOrTerm, …, bool confirm = false)
```

```text
enum ServiceControlStatus { Ok, Denied, NotFound, InvalidState, Timeout, DependentRunning, HasDependents, Unsupported }

readonly record struct ServiceControlResult(
    string Name,
    ServiceControlStatus Status,
    ServiceStatus? ResultingState,
    string? Reason);
```

Default timeout is 30 seconds. `Stop` of a service that has running dependents returns `HasDependents` unless `confirmDependents: true`, in which case SCM stop is issued and dependents are **not** force-killed as processes — SCM owns the cascade.

`Restart` = Stop (wait until Stopped or timeout) then Start. If Stop fails, Start is not attempted.

### 8.5 Change start type

```text
ServiceControlResult SetStartType(string name, ServiceStartType startType, bool confirm = false)
```

`confirm` must be true. Delayed-auto is expressed as `AutomaticDelayed`, not a second call. Disabled → Automatic is allowed. Binary path changes are out of v1.

---

## 9. Campaigns (scheduler)

Same idea as Processes campaigns. The unit of match is a **service name**, not a PID.

```text
record ServiceCampaignRecipe(
    string Id,
    string? DisplayName,
    string Filter,                          // name term or KQL
    ServiceSearchMode? Mode,                // null → treat Filter as KQL
    IReadOnlyList<ServiceCampaignWindow> Windows,
    TimeSpan SampleInterval,                // 250 ms – 60 s
    ServiceWatchFields Fields,
    string? OutputDirectory = null);        // default %ProgramData%\Vestigium\Services\Campaigns\{Id}\

record ServiceCampaignWindow(TimeOnly Start, TimeSpan Duration);
```

Behavior:

- Host calls `ServiceCampaign.Start(recipe)` and leaves the process running.
- Inside an open window the watcher samples matching services and **appends** one JSONL object per sample per service.
- Outside a window the campaign sleeps. Midnight wrap is allowed (`23:55` + 15 min).
- Re-starting the same `Id` must not throw `IOException` because `recipe.json` already exists — overwrite recipe, append samples.
- JSONL must not contain account passwords or the raw service account secret.
- Shared-process rows still emit one line per **service**, with the same Pid and a `SharedProcess` flag.

Example recipe the host should be able to express:

```text
Filter:  Name IN ('Spooler', 'LanmanServer', 'Wuauserv') || Name LIKE 'SQL%'
Windows: 00:00 for 5 min, 03:00 for 2 min, 06:00 for 2 min, 10:00 for 2 min, 12:00 for 5 min
```

---

## 10. KQL binding

v1 binds at least the fields already in `KqlPack.Service`:

| Canonical | Aliases | Type |
|---|---|---|
| `SVC.Name` | `Name` | string |
| `SVC.DisplayName` | | string |
| `SVC.Status` | `State` | string |
| `SVC.StartType` | | string |
| `SVC.Pid` | `PID` | int? |

Implementation of this library **should** grow the catalog (in `Vestigium.Helpers.Kql`) so operators can write the queries they will actually type:

| Canonical | Why |
|---|---|
| `SVC.ImagePath` | find odd binaries |
| `SVC.Account` | find LocalSystem sprawl |
| `SVC.SharedProcess` | svchost grouping |
| `SVC.DelayedAutoStart` | boot noise |
| `SVC.TriggerStart` | “why is this stopped?” |
| `SVC.Kind` | hide drivers in a Win32 view |
| `SVC.DependsOn` | string (comma-joined) or later collection match |
| `SVC.LaunchProtected` | PPL services |

Until those catalog rows exist, KQL that names them must fail compile with `unknown field` and the enabled-field list — same contract as Processes. Do not silently ignore.

Joined process counters stay on `PROC.*` / `CPU.*` / `MEM.*` / `IO.*`. A host that wants `CPU.Usage > 20 && SVC.Name LIKE 'SQL%'` enables **both** packs on one session or runs two filters. v1 does not invent `SVC.Cpu`.

---

## 11. Logging

| Event | Status | Notes |
|---|---|---|
| Probe | Pending → Success | Count of services seen. |
| List / Search | Success | Start + result count. Not every row. |
| Start / Stop / Pause / Continue / Restart | Success or Error | Service name only. |
| SetStartType | Success or Error | Old → new type. |
| Watcher start / stop | Information | Name or query hash, interval. |
| Campaign start / stop / window open / window close | Information | Recipe id. |
| Denied control | Error | Name + reason. No dump of SCM blob. |

Passwords, service account secrets, and full failure-command lines that embed credentials never appear.

---

## 12. Probe

```text
string Probe()
```

Returns `ServiceHelper.Identity`. Side effects: HelperLog Pending/Success, `List(Identity, Win32)` for a count, `TryGet("EventLog")`. No control verbs. No install. No file writes.

---

## 13. Non-goals (v1)

| Out | Why |
|---|---|
| Create / Delete service | Wrong blast radius for a helper used by ProbeHost. |
| Change binary path or account password | Config-write surface deferred. Start type is the only config write. |
| systemd / launchd | Different managers. |
| `IHostedService` / generic host wrappers | Different meaning of “service”. |
| Force-kill of `svchost` via `ProcessHelper.Kill` from this façade | Shared process. Host may call Processes itself after an explicit confirm in *that* library. |
| Per-service CPU isolation inside a shared process | Windows does not give it. Flag `SharedProcess` and tell the truth. |
| UI, tray icon, recovery-dialog automation | Host concern. |
| Demo gallery expansion | Explicitly deferred until the Vestigium solution consumes the library. |

---

## 14. Edge cases the implementation must honor

| Edge | Required behavior |
|---|---|
| Service deleted mid-list | Skip or return Gone on that name. Do not fail the list. |
| Name vs display name | `Get` / Control use **service name**. Search may match display name. |
| Case | SCM names are case-insensitive. Snapshots preserve the SCM-reported casing. |
| Share-process PID reuse | Pid is what SCM reports *now*. Do not cache Pid across Stop → Start without a fresh query. |
| Stop pending forever | Control waits up to timeout, then `Timeout` with the last observed Status. |
| Disabled + Start | `InvalidState` (or SCM’s equivalent), not a hang. |
| Driver with no user-mode PID | Pid stays null. Process join is skipped. |
| Access denied on query-config | Status from enum-status if possible; config fields Denied. |
| Trigger-start service that is Stopped | Status is Stopped. `TriggerStart = true`. Do not treat as broken. |
| AutomaticDelayed at boot | StartType reports AutomaticDelayed when config2 is available; otherwise Automatic + DelayedAutoStart flag. |
| Campaign recipe.json exists | Overwrite recipe, append JSONL. Never throw “destination already exists”. |
| Empty filter | Reject. A campaign that matches every service on the box is not a default. |
| MaxResults | Cap 256, reject above. |

---

## 15. Acceptance (library, not Demo)

A revision is accepted when:

1. `ServiceHelper.Identity` and `Probe()` work on a standard workstation without elevation (query path).
2. `List()` returns Win32 services including Stopped ones.
3. `Get("EventLog")` fills Name, DisplayName, Status, StartType.
4. Search Contains on `spool` hits the Print Spooler by name or display name.
5. KQL `Status == 'Running' && StartType == 'Automatic'` compiles on `KqlPack.Service` and matches at least one live row.
6. `GetDependencyTree` on a service with dependents returns those names and does not loop.
7. Control against a disposable test service (or a documented skip when the box cannot install one) returns typed results; denylist services are never stopped by tests.
8. Watcher raises `Sampled` at least twice in 3 seconds at a 1 s interval.
9. Campaign window math matches Processes (including midnight wrap) and appends JSONL without colliding on `recipe.json`.
10. HelperLog has no password and no per-tick noise.

---

## 16. Open items for the reviewer

Accept / reject these before implementation starts:

| # | Proposal | Default in this draft |
|---|---|---|
| O1 | TFM `net10.0-windows` | **Accept** |
| O2 | Reference Processes for the PID join | **Accept** |
| O3 | Create / Delete out of v1 | **Accept** |
| O4 | Drivers opt-in, default Win32 only | **Accept** |
| O5 | Grow Kql `SVC.*` catalog as listed in §10 | **Accept** — catalog change is a Kql PR that this library consumes |
| O6 | Cascade stop only with `confirmDependents: true` | **Accept** |
| O7 | Campaigns in v1 (not deferred) | **Accept** — same scheduler story as Processes |
| O8 | Change start type in v1 | **Accept** with Confirm |
| O9 | Change account / binary path | **Reject for v1** |
| O10 | Demo work | **Reject for this milestone** |

---

## 17. Document history

| Ver | Date | Notes |
|---|---|---|
| 0.1 | 7 Sep 2026 | Skeleton. Probe-only placeholder. |
| 1.0 | 11 Sep 2026 | First full SRS. Mirrors Processes: snapshot, watcher, search, tree, control, KQL, campaigns. SCM-specific field set and guards. |
