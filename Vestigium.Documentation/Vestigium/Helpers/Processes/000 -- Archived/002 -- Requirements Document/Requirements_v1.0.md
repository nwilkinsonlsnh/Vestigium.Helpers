# Vestigium.Helpers.Processes — Requirements Specification

**Document ID:** VEST-HLP-PROCESSES-SRS-000  
**Version:** 1.1  
**Status:** Draft — first lossless revision. Replaces the 7 September 2026 v1.0 skeleton.  
**Date:** 10 September 2026  
**Package:** `Vestigium.Helpers.Processes`  
**Project ID:** HLP-PRC  
**TFM:** `net10.0-windows` (.NET 10 LTS) — **Windows-only in v1**  
**Companion:** [`DevelopersGuide_v1.0.md`](DevelopersGuide_v1.0.md)  
**Hosts:** ProbeHost, PingIQ / DnsIQ / TraceIQ / HttpIQ (when they need to inspect their own tree), later services, WPF gallery `Vestigium.Helpers.Processes.Demo`

If implementation and this file disagree, this file wins.

This package is a **library of resources**, not a Process Explorer clone and not a shell. Hosts subscribe. It reads the Windows process, thread, memory, I/O, GPU, and security surfaces and publishes typed snapshots. It does not spawn `tasklist.exe`, `taskkill.exe`, `wmic.exe`, `powershell.exe`, Process Explorer, Process Monitor, System Informer, or `procdump`.

---

## 0. How to read this document

It records:

- one façade (`ProcessHelper`) for identity, Probe, list, search, start, kill, tree walk, thread walk, and system counters
- one immutable **process snapshot** (`ProcessInfo`) that carries identity, image, window, security, and resource fields
- one **watcher** contract for fields that change: current sample plus a delta on a caller-chosen interval
- one immutable **thread snapshot** (`ThreadInfo`) per spawned thread
- one **system snapshot** (`SystemCounters`) for machine-wide CPU, commit, physical, kernel, paging, I/O, GPU, and topology
- search that is `StartsWith` / `EndsWith` / `Contains` over a documented field set
- parent/child walking so a host can see a process and the tree it started
- start, start-as, and kill as first-class verbs with an audit line
- HelperLog only; Category `Helpers`; APPID `Processes`
- partial data: unavailable fields are `null` plus an availability flag. Never invent a signer, a parent, or a GPU number

Process Explorer and System Informer are the *column reference*. They are not spawned and they are not a dependency.

---

## 1. Purpose

Give every Vestigium host one way to:

1. List what is running.
2. Describe a process the way an operator already reads Process Explorer.
3. Watch CPU, private bytes, working set, and I/O counters without polling by hand.
4. Walk the child tree.
5. List the threads a process has spawned.
6. Start a process (current token or alternate credentials the caller already holds).
7. Stop a process by PID or by a bounded search result.
8. Read machine-wide CPU / memory / I/O / GPU counters for a summary strip.

Typical hosts: a gallery that looks like a process table; ProbeHost checking whether a child probe is still alive; a service that must not leak handles; an operator killing a stuck helper.

This is not Services. Services talks to SCM. This is not FileIo. FileIo moves files. This is not Network. Network speaks protocols. Processes talks to the OS process table.

```csharp
var snap = ProcessHelper.Get(pid);
var tree = ProcessHelper.GetTree(pid);
using var watch = ProcessHelper.Watch(pid, TimeSpan.FromSeconds(1), ProcessWatchFields.Cpu | ProcessWatchFields.PrivateBytes);
watch.Sampled += (_, s) => ui.Render(s);

var hits = ProcessHelper.Search("vestigium", ProcessSearchMode.Contains, ProcessSearchFields.Name | ProcessSearchFields.ImagePath);
```

---

## 2. Architectural constraints (binding)

| ID | Constraint |
|---|---|
| A1 | Class library `net10.0-windows`. No WPF, no Themes, no Controls, no ScottPlot. The Demo gallery is a separate `net10.0-windows` WPF host. |
| A2 | Independently referenced. Core `Vestigium.Helpers` may be used for guards and `HelperLog` only. `Vestigium.Helpers.WinReg` may be used for Autostart Location. Hashing / Encryption / Analytics / Charts are not referenced in v1. |
| A3 | The library never calls `VestigiumLogger.Initialize`. It never chooses a log folder. Boundary calls write Debug enter and Information constructed/started/killed; rejects write Error then throw. Inner sample loops of a watcher do **not** write JSONL. |
| A4 | Public work lives on `ProcessHelper` (façade) plus immutable snapshots. No static bag of P/Invoke. Native interop is internal. |
| A5 | A `ProcessInfo` is a **snapshot**, not a live `System.Diagnostics.Process` wrapper leaked to the caller. Hosts that want change over time use `IProcessWatcher`. |
| A6 | `System.Diagnostics.Process` is an implementation detail, not the public contract. Many required fields are not on that type. |
| A7 | No process-tool spawn. No `cmd /c taskkill`. No WMI `Win32_Process.Create` as the start path (see §8). |
| A8 | Privileged fields that the current token cannot read are `null` with `Availability = Denied` or `Unsupported`. Do not throw from `List` / `Get` for a single unreadable process. Throw only on contract violations (null name, interval out of range, PID ≤ 0). |
| A9 | Credentials supplied to Start-As are never logged, never written to disk, never kept after `CreateProcess` returns. `NetworkCredential.Password` is cleared by the caller; the library does not retain the string. |
| A10 | Kill is explicit. There is no “kill on dispose” and no implicit tree-kill. Tree-kill is a separate method with a confirmation flag. |
| A11 | Tests must not kill `csrss`, `smss`, `wininit`, `winlogon`, `services`, `lsass`, or the test host. Tests start disposable children under `%TEMP%`. |
| A12 | Linux / macOS are **out of v1**. The umbrella SRS currently lists this project as `net10.0`; v1.1 changes that to `net10.0-windows`. A later portable subset (list / start / kill / PID / name / CPU%) can be extracted if a Linux host appears. |

---

## 3. Decisions locked in this version

| # | Decision | Locked as |
|---|---|---|
| 1 | TFM | **`net10.0-windows`**. The field set below is a Windows process table. Keeping `net10.0` would force stubs for signer, CFG, DEP, integrity, session, package, DPI, GPU, and paging lists. |
| 2 | Cousin tools | Process Explorer / System Informer are the **column reference**. Never spawned. Never a NuGet or native dependency. |
| 3 | Two tempos | **Snapshot** (one shot) and **Watcher** (interval). Watchers exist only for fields that change on a clock: CPU, private bytes, working set, I/O counts/bytes, thread CPU / CSwitch, system deltas. Identity fields are not watched. |
| 4 | Watcher interval | Default **1 second**. Allowed **250 ms – 60 s**. Values outside that range are rejected (`ArgumentOutOfRangeException`). No silent clamp. |
| 5 | CPU meaning | Process and thread **CPU %** is processor-time delta over the watcher interval, divided by interval × logical-processor count, × 100. A single snapshot without a previous sample publishes `CpuPercent = null` and `CpuTime` (raw). |
| 6 | Memory units | Public process memory is **bytes** (`long`). System counters that Process Explorer shows in K are published as **bytes** plus a documented `*K` convenience on the system snapshot. Never mix units on one property. |
| 7 | Partial rows | `List()` never fails the whole table because one protected process refused `QueryLimitedInformation`. That row is present with PID + name and the rest null / Denied. |
| 8 | Search | `StartsWith`, `EndsWith`, `Contains`. Case-insensitive ordinal. No regex in v1. Default fields: Name, ImagePath, CommandLine, WindowTitle. |
| 9 | Tree | Parent is PPID. Children are processes whose PPID equals this PID **at snapshot time**. Orphans (parent already exited, PID reused) are flagged `ParentAlive = false`. |
| 10 | Start-As | Caller passes `ProcessStartAs`. Library uses the Windows create-with-logon path. Profile load is opt-in. No credential prompt UI in the library. |
| 11 | Kill | By PID. Optional `KillTree`. `Search` + kill is host-composed unless the host calls `KillSearch` with `Confirm = true` and a non-empty result cap. |
| 12 | Protected / critical | Kill of a process marked Protected or Critical returns a typed failure (`ProcessKillResult.Denied`). The library does not disable Critical Process. |
| 13 | Comment | Host annotation keyed by image path (normalized) + optional SHA-256 of command line. Stored under `%ProgramData%\Vestigium\Processes\Comments.json` only when the host asks to persist. Default is in-memory for the process lifetime of the host. |
| 14 | Logging door | `HelperLog` only. Category `Helpers`. APPID `Processes`. Sparse: list/search start+count, start, start-as (user only), kill, watcher start/stop. No per-interval sample lines. |
| 15 | GPU | Machine-wide GPU usage / dedicated / system memory plus a per-process GPU sample when the Windows GPU performance counters (or equivalent documented API) are present. Missing GPU stack → `Unsupported`, not zero. |
| 16 | Probe | On-box only. Lists the current process and one disposable child started in `%TEMP%`, then kills that child. Never touches Desktop or Program Files. |

---

## 4. Glossary (binding language)

| Term | Meaning |
|---|---|
| **Snapshot** | Immutable values captured at one instant. |
| **Watcher** | An `IDisposable` that samples selected fields every `Interval` and raises `Sampled`. |
| **Delta** | Value now minus value at the previous sample. First sample of a watcher has delta `null`. |
| **PID** | Process identifier. Not reused as a stable key across the process lifetime of the machine. |
| **PPID** | Parent PID at create time as the kernel still reports it. |
| **Image path** | Full path of the main executable. Empty/null when the token cannot open the image. |
| **Image type** | `X86`, `X64`, `Arm64`, `Unknown`. “x32” in operator speech is `X86`. |
| **Private bytes** | Commit charge of the process (private committed memory). |
| **Working set** | Pages currently resident. |
| **Verified signer** | Authenticode chain result: `Verified`, `NotSigned`, `Untrusted`, `Expired`, `Denied`, `Unknown`. Publisher name when verified. |
| **Protection** | Process protection level (none / light / full / PPL name when known). |
| **Window status** | `None`, `Visible`, `Minimized`, `Maximized`, `Hidden`, `Hung`. |
| **Availability** | `Available`, `Denied`, `Unsupported`, `Gone`. |
| **Gone** | PID was valid at request and exited before the field could be read. |
| **Search mode** | `StartsWith`, `EndsWith`, `Contains`. |
| **Tree** | This process plus descendants by live PPID links. |
| **Start-As** | Create a process under credentials the caller already holds. |
| **System counters** | Machine-wide numbers, not per-process. |

These words are not interchangeable. Host UI copy and XML docs must use them as defined here.

---

## 5. Goals

**G1.** One façade (`ProcessHelper`) owns `Identity`, `Probe`, list, get, search, tree, threads, watch, start, start-as, kill, and system counters.  
**G2.** List every process the token can see, including ones that only allow limited query.  
**G3.** `ProcessInfo` carries the v1 field set in §6.  
**G4.** Watchers exist for CPU, Private Bytes, Working Set, I/O Reads, I/O Read Bytes, I/O Writes, I/O Write Bytes, and the system delta family in §9.  
**G5.** Search is StartsWith / EndsWith / Contains across a documented field mask.  
**G6.** Tree walk returns parent, children, and a flat descendant list with depth.  
**G7.** Thread list returns the §7 field set.  
**G8.** Start and Start-As return a `ProcessStartResult` with the new PID.  
**G9.** Kill and Kill-Tree return a typed result per PID. They do not throw on Access Denied.  
**G10.** System counters cover GPU, I/O deltas, commit, physical, kernel, paging, paging lists, CPU totals, topology, and the four-number summary strip.  
**G11.** Logs meet ALCOA+ through HelperLog. Passwords never appear.  
**G12.** `Probe` is disposable and confined to `%TEMP%`.

---

## 6. Process snapshot (`ProcessInfo`)

Every field below is in scope for v1. Groups exist so a host can request a **slim** list (identity + resources) or a **full** row (image + security + window).

`List()` default is slim. `Get(pid)` default is full. `List(ProcessDetailLevel.Full)` is allowed and may be slower; document that it opens more handles.

### 6.1 Identity

| Field | Type | Notes |
|---|---|---|
| `Pid` | `int` | Required. Always present on a returned row. |
| `ParentPid` | `int?` | PPID. Null only if the kernel did not report one (Idle / System edge cases). |
| `ParentAlive` | `bool?` | Whether a process with that PPID still exists. |
| `Name` | `string` | Image name without path (`svchost.exe`). |
| `SessionId` | `int?` | Terminal session. |

### 6.2 Image and publisher

| Field | Type | Notes |
|---|---|---|
| `ImagePath` | `string?` | Full path. |
| `ImageType` | `ProcessImageType` | `X86` / `X64` / `Arm64` / `Unknown`. |
| `Description` | `string?` | Version resource `FileDescription`. |
| `CompanyName` | `string?` | Version resource `CompanyName`. |
| `Version` | `string?` | File version string. |
| `VerifiedSigner` | `SignerInfo?` | See §6.8. |
| `PackageName` | `string?` | AppX / MSIX package family name when the process is packaged. |
| `CommandLine` | `string?` | Requires sufficient access. May be Denied on protected processes. |
| `Comment` | `string?` | Host annotation (§3.13). |
| `AutostartLocation` | `string?` | Best-effort: Run key, RunOnce, Startup folder, service, scheduled task, or `None`. |

### 6.3 Window

| Field | Type | Notes |
|---|---|---|
| `WindowTitle` | `string?` | Main window title if one exists. |
| `WindowStatus` | `WindowStatus` | See glossary. |

### 6.4 Resources (snapshot + watchable)

On a one-shot snapshot, raw counters are filled and **percent / delta are null** unless the caller also passes a previous snapshot.

| Field | Type | Watchable | Notes |
|---|---|---|---|
| `CpuTime` | `TimeSpan?` | — | Total user+kernel CPU time. |
| `CpuPercent` | `double?` | Yes | See §3.5. |
| `PrivateBytes` | `long?` | Yes | Bytes. |
| `WorkingSet` | `long?` | Yes | Bytes. |
| `IoReads` | `long?` | Yes | Operation count. |
| `IoReadBytes` | `long?` | Yes | Bytes. |
| `IoWrites` | `long?` | Yes | Operation count. |
| `IoWriteBytes` | `long?` | Yes | Bytes. |
| `GpuUsagePercent` | `double?` | Yes | When GPU counters exist. |
| `GpuDedicatedBytes` | `long?` | Yes | |
| `GpuSystemBytes` | `long?` | Yes | |

Watcher samples also carry `*Delta` for each watchable counter (count or bytes since last sample) and `Interval`.

### 6.5 Security and mitigations

| Field | Type | Notes |
|---|---|---|
| `IntegrityLevel` | `IntegrityLevel?` | Untrusted / Low / Medium / MediumPlus / High / System / Protected. |
| `DepStatus` | `DepStatus?` | Enabled / Disabled / Permanent / Unknown. |
| `AslrEnabled` | `bool?` | |
| `ControlFlowGuard` | `MitigationState?` | Enabled / Disabled / ExportSuppressed / Unknown. |
| `StackProtection` | `MitigationState?` | /GS + CET / shadow-stack when queryable. |
| `UiAccess` | `bool?` | Token UIAccess. |
| `Virtualized` | `bool?` | UAC file/registry virtualization. |
| `Protection` | `ProcessProtection?` | None or PPL level / signer when known. |
| `DpiAwareness` | `DpiAwareness?` | Unaware / System / PerMonitor / PerMonitorV2 / UnawareGdiScaled. |
| `EnterpriseContext` | `string?` | AppContainer / enterprise data protection context when present. |

Unavailable mitigations are `null` + `Unsupported` on down-level OS builds. Do not fake CFG on an OS that cannot report it.

### 6.6 Availability

```text
readonly record struct FieldAvailability(ProcessField Field, Availability State, string? Reason);
```

`ProcessInfo.Availability` is the list of fields that are not `Available`. A host can show a lock icon without guessing.

### 6.7 Slim vs full

| Level | Includes |
|---|---|
| `Identity` | PID, PPID, Name, SessionId |
| `Slim` | Identity + resources in §6.4 (no signer, no mitigations, no command line) |
| `Full` | Everything in §6 |

Default `List` = Slim. Default `Get` / `Search` when the host asks for details = Full.

### 6.8 Verified signer

```text
readonly record struct SignerInfo(
    SignerTrust Trust,
    string? Publisher,
    string? Issuer,
    DateTimeOffset? ValidFrom,
    DateTimeOffset? ValidTo);
```

| Trust | Meaning |
|---|---|
| `Verified` | Authenticode chain trusted at query time. |
| `NotSigned` | No signature. |
| `Untrusted` | Signed, chain not trusted. |
| `Expired` | Signed, certificate expired. |
| `Denied` | Token could not read the image. |
| `Unknown` | Query failed for another reason. |

Do not treat catalog-signed OS binaries as `NotSigned` when the catalog verifies.

---

## 7. Threads (`ThreadInfo`)

`ProcessHelper.GetThreads(pid)` returns every thread the token can see for that process.

| Field | Type | Watchable | Notes |
|---|---|---|---|
| `ThreadId` | `int` | — | TID. |
| `ProcessId` | `int` | — | |
| `StartTime` | `DateTimeOffset?` | — | |
| `State` | `ThreadState` | — | Initialized / Ready / Running / Standby / Terminated / Waiting / Transition / Unknown. |
| `WaitReason` | `string?` | — | When State is Waiting. |
| `StartAddress` | `string?` | — | Hex address. |
| `StartModule` | `string?` | — | Module that owns the start address, when resolvable. |
| `Stack` | `string?` | — | Top symbolic frame when symbols are not required; v1 does not load dbghelp PDBs. Module+offset is enough. |
| `CpuPercent` | `double?` | Yes | Same interval rule as process CPU. |
| `ContextSwitches` | `long?` | — | Lifetime count. |
| `ContextSwitchDelta` | `long?` | Yes | |
| `SuspendCount` | `int?` | — | |
| `KernelTime` | `TimeSpan?` | — | |
| `UserTime` | `TimeSpan?` | — | |
| `Cycles` | `long?` | — | |
| `BasePriority` | `int?` | — | |
| `DynamicPriority` | `int?` | — | |
| `IoPriority` | `int?` | — | |
| `MemoryPriority` | `int?` | — | |
| `IdealProcessor` | `int?` | — | |

Stack walks that need to suspend the thread are **opt-in** (`includeStack: true`). Default thread list does not suspend.

---

## 8. Lifetime operations

### 8.1 List and get

```text
IReadOnlyList<ProcessInfo> List(ProcessDetailLevel level = Slim)
ProcessInfo? Get(int pid, ProcessDetailLevel level = Full)
bool TryGet(int pid, out ProcessInfo info)
```

`Get` returns null when the PID does not exist. It does not throw for Gone.

### 8.2 Tree

```text
ProcessTree GetTree(int pid)
IReadOnlyList<ProcessInfo> GetChildren(int pid)
IReadOnlyList<ProcessInfo> GetDescendants(int pid)
```

```text
sealed class ProcessTree
{
    ProcessInfo Root { get; }
    IReadOnlyList<ProcessTree> Children { get; }
    IReadOnlyList<ProcessInfo> Flatten();   // depth-first, includes root
}
```

Cycle guard: if a PPID chain loops because of PID reuse mid-walk, stop that branch and mark `AmbiguousParent`.

### 8.3 Search

```text
IReadOnlyList<ProcessInfo> Search(
    string term,
    ProcessSearchMode mode,
    ProcessSearchFields fields = Name | ImagePath | CommandLine | WindowTitle,
    ProcessDetailLevel level = Slim,
    int maxResults = 256)
```

| Rule | Value |
|---|---|
| Modes | `StartsWith`, `EndsWith`, `Contains` |
| Comparison | Ordinal ignore case |
| Empty / whitespace term | Reject (`HelperGuard`) |
| `maxResults` | Default 256, max 4096, min 1 |
| Regex | Not v1 |

A host that wants to kill search hits composes `Search` then `Kill` / `KillSearch`.

### 8.4 Start

```text
ProcessStartResult Start(ProcessStartRequest request)
ProcessStartResult StartAs(ProcessStartRequest request, ProcessStartAs credentials)
```

`ProcessStartRequest`:

| Field | Default |
|---|---|
| `FileName` | required |
| `Arguments` | none |
| `WorkingDirectory` | inherit |
| `UseShellExecute` | false |
| `CreateNoWindow` | false |
| `RedirectStandardIO` | false (v1 capture of stdout is opt-in; default off) |
| `Verb` | none |
| `Environment` | inherit + optional overlay |

`ProcessStartAs`:

| Field | Notes |
|---|---|
| `UserName` | required |
| `Domain` | optional |
| `Password` | `SecureString` or one-shot `char[]`. Never log. |
| `LoadUserProfile` | default false |
| `LogonFlags` | documented enum; default interactive-network-cleartext equivalent of create-with-logon |

`ProcessStartResult`: `Ok`, `Pid`, `Error` (`FileNotFound`, `AccessDenied`, `LogonFailed`, `InvalidImage`, `Cancelled`, `Unknown`).

Start-As does not pop UAC. LogonFailed is a typed result, not an exception, when the OS rejects the credentials. Contract errors (empty FileName, empty UserName) still throw.

### 8.5 Kill

```text
ProcessKillResult Kill(int pid, bool force = true)
IReadOnlyList<ProcessKillResult> KillTree(int pid, bool force = true)
IReadOnlyList<ProcessKillResult> KillSearch(ProcessSearchRequest search, KillConfirm confirm)
```

| Rule | Value |
|---|---|
| Default kill | Terminate. `force: false` posts WM_CLOSE to the main window and waits up to 5 s, then returns `StillRunning` if alive. |
| Tree | Children first, then root. |
| Protected / Critical | `Denied`, no retry inside the library. |
| Self | Killing the calling PID is rejected (`ArgumentException`) unless `AllowKillSelf = true`. Default false. |
| `KillSearch` | Requires `confirm.Acknowledged == true` and `search` that produced ≤ `confirm.MaxPids` (default 16). Otherwise reject. |

Tests only kill processes the test started.

---

## 9. System counters (`SystemCounters`)

`ProcessHelper.GetSystemCounters()` and `WatchSystem(TimeSpan interval)`.

First snapshot fills absolute values. Deltas are null until the second sample.

### 9.1 Summary strip

| Field | Meaning |
|---|---|
| `CpuPercent` | Machine CPU % over the last interval (or null on first shot). |
| `SystemCommitPercent` | Current / Limit. |
| `PhysicalMemoryPercent` | (Total − Available) / Total. |
| `IoThroughputBytesPerSec` | Read+Write+Other bytes delta / interval. |

### 9.2 GPU

| Field | Notes |
|---|---|
| `GpuUsagePercent` | Combined adapter usage when available. |
| `GpuDedicatedBytes` | |
| `GpuSystemBytes` | |
| `GpuAdapters` | Per-adapter name, usage, dedicated, system. Empty list if Unsupported. |

### 9.3 I/O (machine)

| Field | Snapshot | Delta on watcher |
|---|---|---|
| Read operations | Yes | `ReadDelta` |
| Read bytes | Yes | `ReadBytesDelta` |
| Write operations | Yes | `WriteDelta` |
| Write bytes | Yes | `WriteBytesDelta` |
| Other operations | Yes | `OtherDelta` |
| Other bytes | Yes | `OtherBytesDelta` |

### 9.4 System commit (bytes and K)

| Field | Notes |
|---|---|
| `CommitCurrent` | |
| `CommitLimit` | |
| `CommitPeak` | |
| `CommitChange` | Watcher only (delta). |
| `CommitPeakToLimit` | Peak / Limit, 0–1. |
| `CommitCurrentToLimit` | Current / Limit, 0–1. |

`CommitCurrentK` etc. are `Current / 1024` convenience properties so a gallery can label columns “Physical Memory (K)” without doing its own math.

### 9.5 Physical memory

| Field | Notes |
|---|---|
| `PhysicalTotal` | |
| `PhysicalAvailable` | |
| `CacheWorkingSet` | |
| `KernelWorkingSet` | |
| `DriverWorkingSet` | |

### 9.6 Kernel memory

| Field | Notes |
|---|---|
| `PagedWorkingSet` | |
| `PagedVirtual` | |
| `PagedLimit` | |
| `Nonpaged` | |
| `NonpagedLimit` | |

### 9.7 Paging

| Field | Notes |
|---|---|
| `PageFaultDelta` | Watcher. |
| `PageReadDelta` | Watcher. |
| `PagingFileWriteDelta` | Watcher. |
| `MappedFileWriteDelta` | Watcher. |

### 9.8 Paging lists (bytes)

Zeroed, Free, Modified, ModifiedNoWrite, Standby, Priority0–Priority7, PagedFileModified.

Unsupported on an edition that does not expose list counters → those properties null, not zero.

### 9.9 CPU totals and topology

| Field | Notes |
|---|---|
| `HandleCount` | System-wide. |
| `ThreadCount` | |
| `ProcessCount` | |
| `ContextSwitchDelta` | Watcher. |
| `InterruptDelta` | Watcher. |
| `DpcDelta` | Watcher. |
| `Cores` | Physical cores. |
| `Sockets` | |
| `LogicalProcessors` | |

---

## 10. Watchers

```text
interface IProcessWatcher : IDisposable
{
    int Pid { get; }
    TimeSpan Interval { get; }
    ProcessWatchFields Fields { get; }
    event EventHandler<ProcessSample> Sampled;
    event EventHandler<EventArgs>? Exited;
}

interface ISystemWatcher : IDisposable
{
    TimeSpan Interval { get; }
    event EventHandler<SystemCounters> Sampled;
}
```

| Rule | Value |
|---|---|
| Interval | 250 ms – 60 s, default 1 s |
| First event | Raise once immediately with deltas null, then at each interval with deltas filled. |
| Exited | Watcher raises `Exited` and stops. It does not throw. |
| Thread | Callbacks on a thread-pool thread. Hosts that touch WPF marshal themselves. |
| Overlap | If a sample takes longer than Interval, skip the missed tick; do not queue a backlog. |
| Dispose | Stops the timer. Idempotent. |

`ProcessWatchFields` flags: `Cpu`, `PrivateBytes`, `WorkingSet`, `IoReads`, `IoReadBytes`, `IoWrites`, `IoWriteBytes`, `Gpu`, `All`.

---

## 11. Public surface (v1)

```csharp
public static class ProcessHelper
{
    public static string Identity { get; }   // "Vestigium.Helpers.Processes"
    public static string Probe();

    public static IReadOnlyList<ProcessInfo> List(ProcessDetailLevel level = ProcessDetailLevel.Slim);
    public static ProcessInfo? Get(int pid, ProcessDetailLevel level = ProcessDetailLevel.Full);

    public static ProcessTree GetTree(int pid);
    public static IReadOnlyList<ProcessInfo> GetChildren(int pid);
    public static IReadOnlyList<ProcessInfo> GetDescendants(int pid);

    public static IReadOnlyList<ProcessInfo> Search(
        string term,
        ProcessSearchMode mode,
        ProcessSearchFields fields = ProcessSearchFields.Default,
        ProcessDetailLevel level = ProcessDetailLevel.Slim,
        int maxResults = 256);

    public static IReadOnlyList<ThreadInfo> GetThreads(int pid, bool includeStack = false);

    public static IProcessWatcher Watch(int pid, TimeSpan interval, ProcessWatchFields fields);
    public static ISystemWatcher WatchSystem(TimeSpan interval);

    public static SystemCounters GetSystemCounters();

    public static ProcessStartResult Start(ProcessStartRequest request);
    public static ProcessStartResult StartAs(ProcessStartRequest request, ProcessStartAs credentials);

    public static ProcessKillResult Kill(int pid, bool force = true);
    public static IReadOnlyList<ProcessKillResult> KillTree(int pid, bool force = true);
    public static IReadOnlyList<ProcessKillResult> KillSearch(ProcessSearchRequest search, KillConfirm confirm);

    public static void SetComment(int pid, string? comment, bool persist = false);
}
```

XML docs on every public type. Names above are binding; extra types may exist internally.

---

## 12. Logging

APPID `Processes`. Category `Helpers`. Sparse.

| Event | Status | What is in the line |
|---|---|---|
| Probe | Pending / Success | identity |
| List | Success | `n=` count, level |
| Search | Success | mode + hit count only |
| Get / Tree | Debug enter | pid |
| Watch start / stop | Information | pid, interval |
| Start | Success / Failed | file name, new pid |
| Start-As | Success / Failed | file name, user name, domain, new pid. **Never password.** |
| Kill / KillTree | Success / Failed / Denied | pid, force, tree |
| Reject | Error then throw | empty term, bad interval, pid ≤ 0 |

Do not log command lines by default. An explicit `LogCommandLine = true` on start options is required before arguments appear.

Watcher samples never write JSONL.

---

## 13. Demo gallery

`Vestigium.Helpers.Processes.Demo` is the Windows WPF host.

v1 tabs (normative intent, not pixel layout):

1. **Processes** — table of slim rows, search box (mode + fields), tree view toggle, details pane for the full row.
2. **Watch** — selected PID, interval, sparkline-ready samples (gallery may use Charts later; Processes does not reference Charts).
3. **Threads** — TID table for the selected process.
4. **System** — summary strip + commit / physical / kernel / paging / GPU / topology.
5. **Start / Kill** — start, start-as fields, kill / kill-tree with confirm.

Gallery initializes HelperLog with APPID `Processes`. JSONL lands at `%ProgramData%\Vestigium\Logs\Processes\`.

---

## 14. Tests

| Gate | Rule |
|---|---|
| Identity | `ProcessHelper.Identity == "Vestigium.Helpers.Processes"` |
| Probe | Starts and kills only a child it created under a temp directory |
| List | Returns at least the test host PID |
| Get | Host PID has Name and a non-null ImagePath on a normal user token |
| Search | StartsWith / EndsWith / Contains against the test host name |
| Tree | Child started by the test appears under the test host PID |
| Watch | Two samples at 250 ms produce a non-null CPU or time delta |
| Start | Starts a tiny fixture, waits, exit code 0 |
| Kill | Kills only the fixture the test started |
| System | `LogicalProcessors >= 1`, `PhysicalTotal > 0` |
| Logging | Temp `LogDirectory` only |
| Forbidden | No kill of system-critical PIDs; no live ProgramData comments unless injected |

Windows-latest is the test gate. Linux CI does not compile this project once the TFM moves to `net10.0-windows` (same pattern as Charts / WinReg).

---

## 15. Non-goals (v1)

- Linux / macOS process tables.
- Spawning Sysinternals or `tasklist` / `taskkill`.
- Kernel driver, ELAM, or bypass of PPL.
- Disabling Critical Process or stripping protection.
- Full debugger: PDB download, complete stack traces, breakpoint, memory edit.
- Handle table dumper, memory maps, loaded-module version matrix beyond start module on a thread.
- Packet capture, ETW session designer, Procmon-class file/registry trace.
- Service install / SCM control (that is `Vestigium.Helpers.Services`).
- Credential UI, credential vault, or storing Start-As passwords.
- Remote process control (another machine).
- Regex search, WQL, or “saved filters” files.
- Chart drawing (Charts library).
- Dump file capture (`MiniDumpWriteDump`) — later roadmap.

---

## 16. Roadmap

| Version | Item |
|---|---|
| v1.0 | Skeleton façade (`Identity`, `Probe`) |
| **v1.1** | **This document.** Lossless Windows process / thread / system contract. |
| v1.2 | Implementation of List / Get / Search / Tree / Watch / Start / Kill |
| v1.3 | Full mitigation + signer + autostart + GPU fill; comments persist |
| v1.4 | Dump capture, loaded modules, handle count per process |
| later | Portable `net10.0` subset for Linux hosts; ETW opt-in traces |

---

## 17. Mapping (documentation only — never spawned)

| Operator column / tool | API |
|---|---|
| Process Explorer process table | `List` / `Get` / `ProcessInfo` |
| Process Explorer threads tab | `GetThreads` |
| Process Explorer System Information | `GetSystemCounters` / `WatchSystem` |
| Find Handle or DLL (name search only) | `Search` on Name / ImagePath / CommandLine |
| tasklist | `List` |
| taskkill / taskkill /T | `Kill` / `KillTree` |
| runas / create-with-logon | `StartAs` |
| Performance Monitor process counters | Watcher fields |
| Sigcheck / Authenticode | `VerifiedSigner` |

---

## 18. Open items for acceptance

Resolve these before marking this SRS Accepted. Implementation does not start on an open item.

| # | Question | Proposed default |
|---|---|---|
| O1 | Confirm TFM move `net10.0` → `net10.0-windows` in the csproj, slnx tests, umbrella SRS, and README. | Yes, v1.1. |
| O2 | Autostart Location depth: Run keys + Startup folder only, or also services + scheduled tasks? | Run keys + Startup folder in v1.2; services/tasks v1.3 (Services helper may own the service half). |
| O3 | Stdout/stderr capture on Start. | Opt-in, off by default. Not required to close v1.2. |
| O4 | Comment store format. | JSON file under ProgramData, path injected in tests. |
| O5 | GPU counter provider (PDH vs documented adapter API). | Implementation choice in the Developers Guide; requirement is the fields, not the provider. |
| O6 | Should `KillSearch` exist on the façade or stay host-composed? | Keep on the façade with `KillConfirm` so a gallery cannot fire an unbounded kill. |

---

## 19. Acceptance

This file is a **draft** until Wilkinson Business marks it Accepted.

Acceptance means:

1. The field lists in §6, §7, and §9 are the v1 contract.
2. The façade in §11 is the public surface.
3. The TFM decision in §3.1 is applied to the project file in the same change set as implementation, not before.
4. A Developers Guide companion is updated with call shapes and the native-API notes that do not belong in an SRS.

Until then the compiled helper stays the skeleton (`Identity` + `Probe`). Do not grow the API ahead of acceptance.
