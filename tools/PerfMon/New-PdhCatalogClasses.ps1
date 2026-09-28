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
foreach ($cat in @($data.categories)) {
    $counters = @($cat.counters)
    if ($counters.Count -eq 0) { continue }

    $id = [string]$cat.identifier
    $pdh = [string]$cat.category
    $sb = New-Object System.Text.StringBuilder
    [void]$sb.AppendLine("namespace $ns;")
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("/// <summary>PDH category $pdh. Generated from EventCatalog/pdh-categories.json.</summary>")
    [void]$sb.AppendLine("public static class $id")
    [void]$sb.AppendLine("{")
    [void]$sb.AppendLine("    public const string Category = `"$pdh`";")
    foreach ($c in $counters) {
        $name = ([string]$c.name).Replace('"', '\"')
        [void]$sb.AppendLine("    public const string $([string]$c.identifier) = `"$name`";")
    }
    [void]$sb.AppendLine()
    [void]$sb.AppendLine("    public static IReadOnlyList<string> Counters { get; } =")
    [void]$sb.AppendLine("    [")
    foreach ($c in $counters) {
        [void]$sb.AppendLine("        $([string]$c.identifier),")
    }
    [void]$sb.AppendLine("    ];")
    [void]$sb.AppendLine("}")
    $path = Join-Path $outDir "$id.cs"
    Set-Content -Path $path -Value $sb.ToString() -Encoding UTF8
    [void]$written.Add($path)
}

if ($written.Count -eq 0) {
    Write-Warning "No classes emitted for $Probe. Shard has no counters (category missing or empty on this box)."
    return
}
$written
