# PerfMon PDH tools

These scripts are repo tools. They are not a .csproj. They live here on purpose:

`D:\Source\Clone\Vestigium.Helpers\tools\PerfMon`

| Path | What |
|---|---|
| `tools\PerfMon\Dump-PdhCatalog.ps1` | Raw machine dump |
| `tools\PerfMon\Filter-PdhCatalog.ps1` | Allow-list shard for one probe |
| `tools\PerfMon\dumps\` | Raw dumps (`pdh-all.json`). Gitignored. |
| `tools\PerfMon\fixtures\pdh-mini.json` | Tiny test dump |
| `src\Vestigium.Helpers.PerfMon.<Probe>\EventCatalog\pdh-categories.json` | Shard that the library uses |

Do not write to `C:\Users\...\dumps` or `C:\src\...`.

```powershell
cd D:\Source\Clone\Vestigium.Helpers\tools\PerfMon

# 1. dump this box (creates tools\PerfMon\dumps\pdh-all.json)
.\Dump-PdhCatalog.ps1 -All

# 2. shard into the PageFile project
.\Filter-PdhCatalog.ps1 -Dump pdh-all.json -Probe PageFile
```

Step 2 writes:

`D:\Source\Clone\Vestigium.Helpers\src\Vestigium.Helpers.PerfMon.PageFile\EventCatalog\pdh-categories.json`

If `pdh-all.json` is still under `C:\Users\nwilkinson-admin\dumps`, move it once:

```powershell
New-Item -ItemType Directory -Force dumps | Out-Null
Copy-Item C:\Users\nwilkinson-admin\dumps\pdh-all.json .\dumps\pdh-all.json
```
