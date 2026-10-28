# PerfMon family

Windows performance samples for diagnostic hosts. Not a plot package. Not Task Manager. Not `Vestigium.Helpers.Network`.

Solution folder: `Library/PerfMon`.

| Project | Role | EVENTID |
|---|---|---|
| `Vestigium.Helpers.PerfMon` | Shared sample contract, job runner, counter source | 17000–17499 |
| `Vestigium.Helpers.PerfMon.Network` | Adapter rates plus the 63-object network catalog | 17500–17999 |
| `Vestigium.Helpers.PerfMon.Cpu` | Processor time and queue | 18000–18499 |
| `Vestigium.Helpers.PerfMon.Gpu` | GPU the OS exposes | 18500–18999 |
| `Vestigium.Helpers.PerfMon.Memory` | Commit, available, working set | 19000–19499 |
| `Vestigium.Helpers.PerfMon.Disk` | Physical and logical disk | 19500–19999 |
| `Vestigium.Helpers.PerfMon.PageFile` | Pagefile usage and paging | 20000–20499 |

Long-form documents sit in the child folder, not at this index.

`Vestigium.Helpers.Network` owns ICMP, DNS, routes, inventory, and `SampleCounters` / `WatchAdapter` as protocol-adjacent jobs. PerfMon.Network owns PDH adapter counters over time. Do not merge them.

Live family plan: [`001 -- Implementation Plan/PR02`](001%20--%20Implementation%20Plan/PR02/PR02a%20--%20Implementation%20Plan.md) (typed catalogs from the PDH dump, locked categories only).

