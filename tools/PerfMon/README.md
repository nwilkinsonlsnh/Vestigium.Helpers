# PerfMon PDH dump

Run on a Windows box. Does not sample values. Writes category + counter names plus identifiers for typed catalogs (`NetworkAdapter.BytesTotalPerSec`).

```powershell
cd tools/PerfMon

# Two Network categories
.\Dump-PdhCatalog.ps1 -Category 'Network Adapter','Network Interface' -OutFile .\dumps\network-adapter.json

# Everything on this machine (large)
.\Dump-PdhCatalog.ps1 -All -OutFile .\dumps\pdh-all.json
```

Identifier rules:

- `Bytes Total/sec` → `BytesTotalPerSec`
- `% Usage` → `PercentUsage`
- `Network Adapter` → `NetworkAdapter`

Check the dump into the probe under `EventCatalog/` when we generate the classes. Shared PerfMon does not own these names.
