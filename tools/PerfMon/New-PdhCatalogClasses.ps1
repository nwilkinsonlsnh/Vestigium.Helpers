#Requires -Version 5.1
<#
.SYNOPSIS
  Emit Catalog/<Identifier>.cs from a probe pdh-categories.json shard.

.EXAMPLE
  .\New-PdhCatalogClasses.ps1 -Probe PageFile
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)]
    [ValidateSet('PageFile','Memory','Cpu','Disk','Gpu','Network')]
    [string] $Probe
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path
$project = Join-Path $repoRoot "src\Vestigium.Helpers.PerfMon.$Probe"
$shard = Join-Path $project 'EventCatalog\pdh-categories.json'
$outDir = Join-Path $project 'Catalog'
$ns = "Vestigium.Helpers.PerfMon.$Probe"

if (-not (Test-Path $shard)) { throw "Shard not found: $shard" }
New-Item -ItemType Directory -Force -Path $outDir | Out-Null

$data = Get-Content -Path $shard -Raw -Encoding UTF8 | ConvertFrom-Json
$written = New-Object System.Collections.Generic.List[string]
$nl = "`n"

foreach ($cat in @($data.categories)) {
    $counters = @($cat.counters)
    if ($counters.Count -eq 0) { continue }

    $id = [string]$cat.identifier
    $pdh = [string]$cat.category
    $lines = New-Object System.Collections.Generic.List[string]
    [void]$lines.Add("namespace $ns;")
    [void]$lines.Add("")
    [void]$lines.Add("/// <summary>PDH category $pdh. Generated from EventCatalog/pdh-categories.json.</summary>")
    [void]$lines.Add("public static class $id")
    [void]$lines.Add("{")
    [void]$lines.Add('    public const string Category = "' + $pdh + '";')
    foreach ($c in $counters) {
        $name = ([string]$c.name).Replace('\', '\\').Replace('"', '\"')
        [void]$lines.Add('    public const string ' + ([string]$c.identifier) + ' = "' + $name + '";')
    }
    [void]$lines.Add("")
    [void]$lines.Add("    public static IReadOnlyList<string> Counters { get; } =")
    [void]$lines.Add("    [")
    for ($i = 0; $i -lt $counters.Count; $i++) {
        $ident = [string]$counters[$i].identifier
        if ($i -lt ($counters.Count - 1)) {
            [void]$lines.Add("        $ident,")
        }
        else {
            [void]$lines.Add("        $ident")
        }
    }
    [void]$lines.Add("    ];")
    [void]$lines.Add("}")
    $text = ($lines -join $nl) + $nl

    $path = Join-Path $outDir "$id.cs"
    $existing = ""
    if (Test-Path $path) {
        $existing = [System.IO.File]::ReadAllText($path)
    }
    $norm = { param($s) if ($null -eq $s) { return "" }; return (($s -replace "`r`n", "`n") -replace "`r", "`n") }
    if ((& $norm $existing) -ne (& $norm $text)) {
        $utf8 = New-Object System.Text.UTF8Encoding $false
        [System.IO.File]::WriteAllText($path, $text, $utf8)
    }
    [void]$written.Add($path)
}

if ($written.Count -eq 0) {
    Write-Warning "No classes emitted for $Probe. Shard has no counters (category missing or empty on this box)."
    return
}
$written
