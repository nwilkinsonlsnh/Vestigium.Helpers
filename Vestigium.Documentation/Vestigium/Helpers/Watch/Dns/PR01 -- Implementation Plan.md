# Vestigium.Helpers.Watch.Dns — PR01 Implementation Plan

**Document ID:** VEST-HELPERS-WATCH-DNS-PR01-PLAN
**Host:** `Vestigium.Helpers.Watch.Dns`
**APPID:** `Watch.Dns`
**Repo:** `nwilkinsonlsnh/Vestigium.Helpers`
**Status:** Live. PR01-05 done. Unseen line is next.
**Date:** 8 October 2026
**Binding:** [PR01 -- Requirements.md](PR01%20--%20Requirements.md) wins on this cut. This file wins on order.

**Goal:** Ship the elevated watch exe. DnsIQ does not call it in this PR.

**Not:** The DnsIQ tab. A service. A hidden console. A DLL. An Operational-log enabler.

**Order against DnsIQ:** Finish this plan. Then open DnsIQ PR08. A thread that starts at DnsIQ PR08 before these slices are closed is on the wrong repo.

---

## Starting point

No project. No pipe. No session. The decision is in the requirements file.

---

## Decision

| Call | Why |
|---|---|
| `src/Vestigium.Helpers.Watch.Dns` | Matches the other helper hosts. Exe, not a class library. |
| Args in, rows out | Duration, mode, pipe name. No config file in this PR. |
| 3008 before packets | The caller and the name are the point. Packets are the second slice. |
| Tests off the wire | Clock, arg reject, and row line. No live ETW in the unit tests. |

---

## Implementation table

| Slice | Id | Work | Status |
|---|---|---|---|
| 1 | PR01-01 | WinExe project. Manifest `requireAdministrator`. No window. Unelevated exit. | Done. Exit 2. |
| 2 | PR01-02 | Clock. Default 5. Step 5. Max 180. Reject the rest. Stop flag. | Done. Accept table. Stop does not wait. |
| 3 | PR01-03 | Named pipe. UTF-8 lines. ACL is starting user and Administrators. | Done. One JSON line. |
| 4 | PR01-04 | Resolver mode. Event 3008 only. Failure line if the session cannot start. | Done. Exit 3. |
| 5 | PR01-05 | Packet mode. UDP/53 and TCP/53. Off unless asked. Empty name if the payload is not a question. | Done. Default stays resolver. |
| 6 | PR01-06 | First line states the mode and what it does not see. Exit code if the pipe cannot open. | |

---

## Slices

### PR01-01

Create `src/Vestigium.Helpers.Watch.Dns/Vestigium.Helpers.Watch.Dns.csproj`. `OutputType` is `WinExe`. Target `net10.0-windows`. Application manifest `requestedExecutionLevel` is `requireAdministrator`. No `AllocConsole`. A run that is not elevated exits `2` before any sensor.

### PR01-02

Done. Missing seconds is 5. Off-step rejects. `Stop` completes a 180 second clock without waiting it. `Main` does not sleep on the clock.

### PR01-03

Done. `WatchPipe.Create` serves one pipe. Windows ACL is the current user and Administrators. A row is one JSON line. The test reads it back. No ETW.

### PR01-04

Done. Provider `{1C95126E-7EEA-49A9-A3FE-A378B03DDB4D}`. Event 3008 only. Pid is the event process id. A session that throws writes one failure row and returns 3. No `wevtutil`. Operational channel is not enabled.

### PR01-05

Done. Arg `packet` binds UDP/53 and TCP/53. A response or a short payload is an empty name and status `Not a question`. No arg stays on the resolver session.

### PR01-06

Before any event, write the unseen line. Resolver text: raw sockets and non-Windows DoH are not in this watch. Packet text: DoH, DoT, and DoQ are not in this watch. If the pipe cannot be created, exit `4` and write nothing.

---

## Files this plan expects to touch

```
src/Vestigium.Helpers.Watch.Dns/Vestigium.Helpers.Watch.Dns.csproj          [NEW]
src/Vestigium.Helpers.Watch.Dns/app.manifest                                [NEW]
src/Vestigium.Helpers.Watch.Dns/Program.cs                                  [NEW]
src/Vestigium.Helpers.Watch.Dns/WatchClock.cs                               [NEW]
src/Vestigium.Helpers.Watch.Dns/WatchPipe.cs                                [NEW]
src/Vestigium.Helpers.Watch.Dns/ResolverWatch.cs                            [NEW]
src/Vestigium.Helpers.Watch.Dns/PacketWatch.cs                              [NEW]
tests/Vestigium.Helpers.Watch.Dns.Tests/WatchClockTests.cs                  [NEW]
tests/Vestigium.Helpers.Watch.Dns.Tests/WatchPipeTests.cs                   [NEW]
```

Do not edit DnsIQ. Do not edit Helpers.Network.

---

## What each watch

| Role | Watch |
|---|---|
| Alvin | Exe, not a DLL. No window. No service. |
| Theodore | Unelevated exit. Bad duration rejects. Sensor failure is a row, not a hang. |
| Simon | 3008 only in resolver mode. Packet mode is opt-in. The unseen line is the first line. |

---

## Next action

PR01-06. First line states the mode and what it does not see. Exit 4 if the pipe cannot open. Do not open DnsIQ PR08 in that turn.
