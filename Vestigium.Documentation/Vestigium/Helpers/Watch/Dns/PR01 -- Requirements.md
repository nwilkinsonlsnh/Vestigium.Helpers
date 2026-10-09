# Vestigium.Helpers.Watch.Dns — PR01 Requirements

**Document ID:** VEST-HELPERS-WATCH-DNS-PR01-REQ
**Host:** `Vestigium.Helpers.Watch.Dns`
**APPID:** `Watch.Dns`
**Repo:** `nwilkinsonlsnh/Vestigium.Helpers`
**Status:** Live. Binding for PR01 until the implementation plan closes it.
**Date:** 8 October 2026
**Consumer, later:** DnsIQ PR08. That paper does not start until this PR01 is closed.

**One sentence:** An elevated, windowless process that watches outbound DNS for a clock the caller sets, and writes rows down a pipe.

**This version is not** a library loaded into DnsIQ. Not a Windows service. Not a hidden console. Not a claim that every name the machine tried is in the file. Not the DnsIQ pulse. Not a resident watcher.

---

## 0. Decision

| Call | Why |
|---|---|
| Name is `Watch.Dns` | Family slot is `Watch.*`. This cut is Dns only. A 180 second sample is a watch, not a resident. |
| Exe, not a DLL | A library cannot elevate. The process token is the process. DnsIQ stays unelevated. |
| Windows subsystem | No console window while the clock runs. The caller is the only surface. |
| Manifest `requireAdministrator` | The sensors do not open without it. UAC is the start, shown once by the caller via `runas`. |
| First sensor is event 3008 | `Microsoft-Windows-DNS-Client` `{1C95126E-7EEA-49A9-A3FE-A378B03DDB4D}`. Query completed. Name, type, status, answers. Process id is the caller, not `svchost`. |
| Port 53 is a second mode | Cleartext UDP/53 and TCP/53, including a bypass of the Windows resolver. Labeled packets. Not the default. |
| Pipe, not a file | A five-second sample does not land in ProgramData. Named pipe. ACL is the starting user and Administrators. |
| Failures ride the pipe | No console to read. A start failure is a row, then exit. Exit code is the backup if the pipe never opens. |

Rejected: `Vestigium.Helpers.DnsIQ.Monitoring` and `Watcher.Dns`. One reads as a DnsIQ library. The other says a resident. Rejected: a hidden console. It eats the error. Rejected: a service. Wrong lifetime. Rejected: enabling the Operational channel. That is a machine change, off by default, 1 MB. Rejected: `pktmon` as the only sensor. It does not yield the caller.

---

## 1. What this version is

One exe. Two modes. One clock. One pipe.

| Piece | PR01 |
|---|---|
| Host | `Vestigium.Helpers.Watch.Dns.exe`. `WinExe`. No window. |
| Elevation | Manifest requires administrator. Refuses to run otherwise. |
| Clock | Default 5 seconds. Step 5. Max 180. Stop ends it early. |
| Default mode | Resolver. Event 3008 only. |
| Second mode | Packets. UDP/53 and TCP/53. Off unless the caller asks. |
| Row | Time, process, pid, name, type, status, answers, mode. |
| Unseen | The exe says so on the first row of a run. Raw sockets and non-Windows DoH are absent from resolver mode. Encrypted DNS is absent from packet mode. |
| Pipe | One server pipe per run. Caller connects. Rows are lines. Process exits when the clock ends, the caller disconnects, or stop is signaled. |

---

## 2. Requirements

### R01-01 The process is windowless and elevated

The project output is a Windows-subsystem exe. The manifest requests `requireAdministrator`. Started without elevation, it exits non-zero and does not open a sensor. It does not allocate a console.

### R01-02 The clock is 5 / 5 / 180

Missing duration means 5 seconds. A value not on a 5 second step is rejected. A value over 180 is rejected. Zero is rejected. Stop before the clock ends the session.

### R01-03 Resolver mode is event 3008

The session enables `Microsoft-Windows-DNS-Client` and keeps event 3008. Query name, type, status, and results are on the row. The process id on the event is the client. Other event ids are dropped. The Operational channel is not enabled.

### R01-04 Packet mode is labeled and off by default

Packet mode captures cleartext DNS on UDP/53 and TCP/53 only. The row mode is `packet`. A name that cannot be read is an empty name, not a guessed name. This mode does not run unless the argument asks for it.

### R01-05 The pipe is the product

Rows are UTF-8 lines on a named pipe. The pipe ACL grants the starting user and Administrators. No other write. No file is written for the sample. A sensor that cannot start writes one failure line and exits. If the pipe cannot be created, the process exits non-zero and writes nothing.

### R01-06 A quiet watch is not proof

The first line of a run states the mode and what that mode does not see. Resolver mode does not see a raw socket or DoH that bypassed the Windows resolver. Packet mode does not see DoH, DoT, or DoQ.

---

## 3. Must not change

- DnsIQ. This PR does not edit `Vestigium.Suite.Network`.
- The Operational log. Do not enable it.
- Helpers.Network. Do not add a capture type there.
- No service install. No tray icon. No window.

---

## 4. Done

A new thread can build from the implementation plan. PR01-01 through PR01-06 are the order. DnsIQ PR08 is blocked until those slices are closed.
