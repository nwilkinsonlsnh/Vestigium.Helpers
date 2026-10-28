# Processes — Requirements addendum 1.3

**Parent:** [`Requirements_v1.0.md`](Requirements_v1.0.md)  
**Status:** Accepted — implementation matches this addendum. Tests passing 15 September 2026.

## Locked behavior

| Topic | Rule |
|---|---|
| Missing PID | `Get` / `TryGet` return null / false. No fake row. Availability `Gone` only when a handle existed and the process then exited. |
| Search cap | `maxResults` 1..4096. Over cap or `<= 0` → `ArgumentOutOfRangeException`. |
| Campaign cap | `MaxMatches` 1..256. Over cap → `ArgumentOutOfRangeException`. |
| Thread state | `ThreadInfo.State` is `ThreadState` (avoids clash with `System.Threading.ThreadState` when tests alias). `ProcessThreadState` exists with the same numeric values. |
| Kql search | `Search(query)` uses `KqlPack.Process`. Short names `WindowTitle`, `CommandLine`, `Name`, `PID` bind. |
| Campaigns | Recipe + windows (start + duration + days). JSONL samples while a window is open. |
