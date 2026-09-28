#Requires -Version 5.1
<#
.SYNOPSIS
  Filter a Dump-PdhCatalog JSON to one probe allow-list.

.EXAMPLE
  .\Filter-PdhCatalog.ps1 -Dump .\dumps\pdh-all.json -AllowList @('Paging File') -OutFile .\shards\pagefile.json
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $Dump,
    [Parameter(Mandatory)][string[]] $AllowList,
    [Parameter(Mandatory)][string] $OutFile
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$dumpPath = [System.IO.Path]::GetFullPath($Dump)
if (-not (Test-Path $dumpPath)) { throw "Dump not found: $dumpPath" }

$raw = Get-Content -Path $dumpPath -Raw -Encoding UTF8
$data = $raw | ConvertFrom-Json
$wanted = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
foreach ($name in $AllowList) {
    $trim = $name.Trim()
    if ($trim) { [void]$wanted.Add($trim) }
}

$rows = New-Object System.Collections.Generic.List[object]
foreach ($cat in @($data.categories)) {
    if (-not $wanted.Contains([string]$cat.category)) { continue }
    $counters = @()
    foreach ($c in @($cat.counters)) {
        $counters += [pscustomobject]@{
            name       = [string]$c.name
            identifier = [string]$c.identifier
        }
    }
    $rows.Add([pscustomobject]@{
        category   = [string]$cat.category
        identifier = [string]$cat.identifier
        type       = [string]$cat.type
        counters   = $counters
    })
}

$payload = [pscustomobject]@{
    source        = [System.IO.Path]::GetFileName($dumpPath)
    machine       = [string]$data.machine
    utc           = [string]$data.utc
    allowList     = @($AllowList)
    categoryCount = $rows.Count
    categories    = $rows
}

$full = [System.IO.Path]::GetFullPath($OutFile)
$dir = Split-Path -Parent $full
if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
Set-Content -Path $full -Value ($payload | ConvertTo-Json -Depth 6) -Encoding UTF8
Write-Output $full
