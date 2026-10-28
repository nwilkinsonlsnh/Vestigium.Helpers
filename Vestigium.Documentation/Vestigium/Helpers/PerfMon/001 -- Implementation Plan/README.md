# 001 -- Implementation Plan

**Family:** Vestigium.Helpers.PerfMon*
**Status:** PR02 landed. PR03 open. Dump-gated catalog fill.

Live paper:

- [`PR03/PR03 -- Backlog.md`](PR03/PR03%20--%20Backlog.md)
- [`PR03/PR03a -- Implementation Plan.md`](PR03/PR03a%20--%20Implementation%20Plan.md) — generator hygiene
- [`PR03/PR03b -- Implementation Plan.md`](PR03/PR03b%20--%20Implementation%20Plan.md) — Gpu + Cpu Information/Performance
- [`PR03/PR03c -- Implementation Plan.md`](PR03/PR03c%20--%20Implementation%20Plan.md) — remaining Disk / Memory / Network objects

PR02 typed the USNCDTWIN01 hits. PR03 does not widen `*Objects.All`. It only emits a class when a shard has counters.
