#Requires -Version 5.1
<#
.SYNOPSIS
  Dump Windows PDH category and counter names for Vestigium.Helpers.PerfMon typed catalogs.

.DESCRIPTION
  IntelliSense types use identifiers like NetworkAdapter.BytesTotalPerSec.
  This script writes the PDH spelling plus that identifier so a later generator
  can emit the classes. It does not sample values.

.EXAMPLE
  .\Dump-PdhCatalog.ps1 -Category 'Network Adapter','Network Interface'

.EXAMPLE
  .\Dump-PdhCatalog.ps1 -All -OutFile .\pdh-catalog.json
#>
[CmdletBinding()]
param(
    [string[]] $Category,
    [switch] $All,
    [string] $OutFile,
    [int] $InstanceCap = 256
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Continue'

function ConvertTo-Identifier {
    param([Parameter(Mandatory)][string] $Name)
    $t = $Name.Trim()
    $t = $t -replace '%\s*', 'Percent '
    $t = $t -replace '/[Ss]ec', ' PerSec'
    $t = $t -replace '[^A-Za-z0-9]+', ' '
    $parts = $t.Split(' ', [System.StringSplitOptions]::RemoveEmptyEntries)
    -join ($parts | ForEach-Object {
        if ($_.Length -eq 1) { $_.ToUpperInvariant() }
        else { $_.Substring(0, 1).ToUpperInvariant() + $_.Substring(1) }
    })
}

function Get-CounterNames {
    param([Parameter(Mandatory)][System.Diagnostics.PerformanceCounterCategory] $Pcc)
    $names = New-Object System.Collections.Generic.List[string]
    try {
        if ($Pcc.CategoryType -eq [System.Diagnostics.PerformanceCounterCategoryType]::MultiInstance) {
            $instances = @($Pcc.GetInstanceNames())
            $pick = $instances | Where-Object { $_ -eq '_Total' } | Select-Object -First 1
            if (-not $pick -and $instances.Count -gt 0) { $pick = $instances[0] }
            if ($pick) {
                foreach ($c in $Pcc.GetCounters($pick)) { [void]$names.Add($c.CounterName) }
            }
        }
        else {
            foreach ($c in $Pcc.GetCounters()) { [void]$names.Add($c.CounterName) }
        }
    }
    catch {
        Write-Warning ("Counters unavailable for '{0}': {1}" -f $Pcc.CategoryName, $_.Exception.Message)
    }
    $names | Sort-Object -Unique
}

if (-not $All -and -not $Category) {
    $here = $PSScriptRoot
    Write-Host @"
Dump-PdhCatalog.ps1
Run from any directory. Do not put 'cd tools/PerfMon' inside this file.

  $here\Dump-PdhCatalog.ps1 -Category 'Network Adapter','Network Interface' -OutFile $here\dumps
etwork-adapter.json
  $here\Dump-PdhCatalog.ps1 -All -OutFile $here\dumps\pdh-all.json
"@
    exit 1
}

$wanted = $null
if ($Category) {
    $wanted = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    foreach ($name in $Category) { [void]$wanted.Add($name.Trim()) }
}

$sets = [System.Diagnostics.PerformanceCounterCategory]::GetCategories() |
    Sort-Object CategoryName

$rows = New-Object System.Collections.Generic.List[object]
foreach ($pcc in $sets) {
    if ($wanted -and -not $wanted.Contains($pcc.CategoryName)) { continue }

    $instances = @()
    try {
        if ($pcc.CategoryType -eq [System.Diagnostics.PerformanceCounterCategoryType]::MultiInstance) {
            $instances = @($pcc.GetInstanceNames() | Select-Object -First $InstanceCap)
        }
    }
    catch {
        Write-Warning ("Instances unavailable for '{0}': {1}" -f $pcc.CategoryName, $_.Exception.Message)
    }

    $counters = @(Get-CounterNames $pcc | ForEach-Object {
        [pscustomobject]@{
            name       = $_
            identifier = ConvertTo-Identifier $_
        }
    })

    $rows.Add([pscustomobject]@{
        category   = $pcc.CategoryName
        identifier = ConvertTo-Identifier $pcc.CategoryName
        type       = $pcc.CategoryType.ToString()
        help       = $(try { $pcc.CategoryHelp } catch { '' })
        instances  = @($instances)
        counters   = $counters
    })
}

$payload = [pscustomobject]@{
    machine    = $env:COMPUTERNAME
    utc        = [DateTimeOffset]::UtcNow.ToString('o')
    categoryCount = $rows.Count
    categories = $rows
}

$json = $payload | ConvertTo-Json -Depth 6

if ($OutFile) {
    $full = [System.IO.Path]::GetFullPath($OutFile)
    $dir = Split-Path -Parent $full
    if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
    Set-Content -Path $full -Value $json -Encoding UTF8
    Write-Output $full
}
else {
    $json
}
