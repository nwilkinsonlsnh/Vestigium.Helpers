# PerfMon PDH dump

Run on a Windows box. Does not sample values. Writes category + counter names plus identifiers for typed catalogs (`NetworkAdapter.BytesTotalPerSec`).

You are already in `tools\PerfMon`. Call the script. Do not `cd tools/PerfMon` again and do not paste that `cd` into the `.ps1`.

```powershell
# from tools\PerfMon
.\Dump-PdhCatalog.ps1 -Category 'Network Adapter','Network Interface' -OutFile .\dumps\network-adapter.json

# everything on this machine (large)
.\Dump-PdhCatalog.ps1 -All -OutFile .\dumps\pdh-all.json
```

From the repo root:

```powershell
.\tools\PerfMon\Dump-PdhCatalog.ps1 -All -OutFile .\tools\PerfMon\dumps\pdh-all.json
```

Identifier rules:

- `Bytes Total/sec` → `BytesTotalPerSec`
- `% Usage` → `PercentUsage`
- `Network Adapter` → `NetworkAdapter`


## Filter a dump to one probe

From `tools\PerfMon`. `-Probe` writes under the repo `src` tree. Do not pass `\src\...` — that is `C:\src\...`.

```powershell
.\Filter-PdhCatalog.ps1 -Dump .\dumps\pdh-all.json -Probe PageFile
.\Filter-PdhCatalog.ps1 -Dump .\dumps\pdh-all.json -Probe Network
```

That lands at:

`D:\Source\Clone\Vestigium.Helpers\src\Vestigium.Helpers.PerfMon.PageFile\EventCatalog\pdh-categories.json`

The shard keeps category, identifier, type, and counters. It drops instances and help. Categories not on the probe allow-list (CLR, SQL) are omitted.
