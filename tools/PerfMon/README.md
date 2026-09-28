# PerfMon PDH tools

Repo tools, not a .csproj. Home:

`D:\Source\Clone\Vestigium.Helpers\tools\PerfMon`

| Path | What |
|---|---|
| `Dump-PdhCatalog.ps1` | Raw machine dump |
| `Filter-PdhCatalog.ps1` | Allow-list shard for one probe |
| `New-PdhCatalogClasses.ps1` | Shard → `Catalog/<Identifier>.cs` |
| `dumps\` | Raw `pdh-all.json` (gitignored) |
| `fixtures\pdh-mini.json` | Tiny test dump |

Shards the library compiles live under:

`src\Vestigium.Helpers.PerfMon.<Probe>\EventCatalog\pdh-categories.json`

Do not write to `C:\Users\...\dumps` or `C:\src\...`.

```powershell
cd D:\Source\Clone\Vestigium.Helpers\tools\PerfMon

.\Dump-PdhCatalog.ps1 -All
.\Filter-PdhCatalog.ps1 -Dump pdh-all.json -Probe PageFile
.\New-PdhCatalogClasses.ps1 -Probe PageFile
```

Same `-Probe` values: `PageFile`, `Memory`, `Cpu`, `Disk`, `Gpu`, `Network`.

Empty dump categories do not get a class. CLR / SQL never enter these probes.
