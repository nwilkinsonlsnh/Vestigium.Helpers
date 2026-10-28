#Requires -Version 5.1
<#
.SYNOPSIS
  Filter a Dump-PdhCatalog JSON to one probe allow-list.

.EXAMPLE
  .\Filter-PdhCatalog.ps1 -Dump .\dumps\pdh-all.json -Probe PageFile

.EXAMPLE
  .\Filter-PdhCatalog.ps1 -Dump .\dumps\pdh-all.json -AllowList @('Paging File') -OutFile .\shards\pagefile.json
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string] $Dump,
    [string[]] $AllowList,
    [string] $OutFile,
    [ValidateSet('PageFile','Memory','Cpu','Disk','Gpu','Network')]
    [string] $Probe
)

Set-StrictMode -Version Latest
$ErrorActionPreference = 'Stop'

$repoRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..')).Path

$probeAllow = @{
    PageFile = @('Paging File')
    Memory   = @('Cache', 'Hyper-V Dynamic Memory Integration Service', 'Memory', 'NUMA Node Memory', 'ReadyBoost Cache')
    Cpu      = @('Processor', 'Processor Information', 'Processor Performance')
    Disk     = @(
        'FileSystem Disk Activity', 'LogicalDisk', 'Ntfs Bucketized Performance', 'PhysicalDisk',
        'ReFS', 'ReFS Bucketized Performance', 'ReFS Dedup Minstore Perf Counters', 'ReFS Dedup Perf Counters',
        'Storage Management WSP Spaces Runtime', 'Storage Spaces Drt', 'Storage Spaces Tier',
        'Storage Spaces Virtual Disk', 'Storage Spaces Virtual Disk Io', 'Storage Spaces Virtual Disk Map',
        'Storport Unit Queue', 'Storport Unit Reads', 'Storport Unit Transfers', 'Storport Unit Writes',
        'VHD Bucketized Performance'
    )
    Gpu      = @('GPU Engine', 'GPU Process Memory', 'GPU Adapter Memory', 'GPU Local Adapter Memory', 'GPU Non Local Adapter Memory')
    Network  = @(
        'Per Processor Network Activity Cycles', 'Per Processor Network Interface Card Activity',
        'TCPIP Performance Diagnostics (Per-CPU)', 'Bluetooth Device', 'Bluetooth Radio', 'DNS64 Global',
        'Firewall Rules by Profile', 'Firewall Rules by Store', 'HTTP Service', 'HTTP Service Request Queues',
        'HTTP Service Url Groups', 'Hyper-V Virtual Machine Bus Pipes', 'ICMP', 'ICMPv6', 'IPHTTPS Global',
        'IPHTTPS Session', 'IPsec AuthIP IPv4', 'IPsec AuthIP IPv6', 'IPsec Connections', 'IPsec Driver',
        'IPsec IKEv1 IPv4', 'IPsec IKEv1 IPv6', 'IPsec IKEv2 IPv4', 'IPsec IKEv2 IPv6', 'IPv4', 'IPv6',
        'Network Adapter', 'Network Interface', 'Network QoS Policy', 'PacketDirect EC Utilization',
        'PacketDirect Queue Depth', 'PacketDirect Receive Counters', 'PacketDirect Receive Filters',
        'PacketDirect Transmit Counters', 'Physical Network Interface Card Activity', 'RemoteFX Network',
        'SMB Client Shares', 'SMB Direct', 'SMB Server', 'SMB Server Sessions', 'SMB Server Shares',
        'TCPIP Extended Performance Diagnostics', 'TCPIP Performance Diagnostics',
        'TCPIP Transport Layer Packet Drop Counters', 'TCPv4', 'TCPv6', 'Teredo Client', 'Teredo Relay',
        'Teredo Server', 'UDPv4', 'UDPv6', 'WFP', 'WFP Classify', 'WFP Filter Count', 'WFP Filter Size',
        'WFP Reauthorization', 'WFPv4', 'WFPv6', 'WinNAT', 'WinNAT ICMP', 'WinNAT Instance', 'WinNAT TCP',
        'WinNAT UDP'
    )
}

if ($Probe) {
    $AllowList = $probeAllow[$Probe]
    if (-not $OutFile) {
        $OutFile = Join-Path $repoRoot "src\Vestigium.Helpers.PerfMon.$Probe\EventCatalog\pdh-categories.json"
    }
}

if (-not $AllowList -or $AllowList.Count -eq 0) {
    throw "Pass -Probe PageFile (or Memory/Cpu/Disk/Gpu/Network) or an explicit -AllowList."
}
if (-not $OutFile) {
    throw "Pass -OutFile or -Probe so the shard lands under src\\Vestigium.Helpers.PerfMon.<Probe>\\EventCatalog."
}

function Resolve-DumpPath {
    param([string] $Dump)
    if ([System.IO.Path]::IsPathRooted($Dump) -and (Test-Path $Dump)) { return $Dump }
    $candidates = @(
        (Join-Path (Get-Location) $Dump),
        (Join-Path $PSScriptRoot $Dump),
        (Join-Path $PSScriptRoot (Join-Path 'dumps' $Dump)),
        (Join-Path $PSScriptRoot 'dumps\pdh-all.json'),
        (Join-Path $PSScriptRoot 'pdh-all.json'),
        (Join-Path $repoRoot $Dump),
        (Join-Path $repoRoot (Join-Path 'tools\PerfMon\dumps' $Dump))
    )
    foreach ($path in $candidates) {
        if ($path -and (Test-Path $path)) { return (Resolve-Path $path).Path }
    }
    throw "Dump not found: $Dump. Pass the full path to pdh-all.json (it is not in git)."
}

$dumpPath = Resolve-DumpPath $Dump

$outPath = $OutFile
if (-not [System.IO.Path]::IsPathRooted($outPath)) {
    $outPath = [System.IO.Path]::GetFullPath((Join-Path (Get-Location) $outPath))
}
if ($outPath -match '^[A-Za-z]:\\src\\') {
    throw "Refusing $outPath. Use -Probe PageFile so the file goes under $repoRoot\src\..."
}

$raw = Get-Content -Path $dumpPath -Raw -Encoding UTF8
$data = $raw | ConvertFrom-Json
$wanted = @{}
foreach ($name in @($AllowList)) {
    $trim = ([string]$name).Trim()
    if ($trim) { $wanted[$trim.ToLowerInvariant()] = $true }
}

$rows = New-Object System.Collections.Generic.List[object]
foreach ($cat in @($data.categories)) {
    $key = ([string]$cat.category).ToLowerInvariant()
    if (-not $wanted.ContainsKey($key)) { continue }
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

function Escape-JsonText {
    param([string] $Text)
    if ($null -eq $Text) { return '""' }
    $safe = $Text.Replace('\', '\\').Replace('"', '\"')
    return ('"' + $safe + '"')
}

$nl = [Environment]::NewLine
$sb = New-Object System.Text.StringBuilder
[void]$sb.Append("{" + $nl)
[void]$sb.Append("  " + '"source": ' + (Escape-JsonText ([System.IO.Path]::GetFileName($dumpPath))) + "," + $nl)
[void]$sb.Append("  " + '"machine": ' + (Escape-JsonText ([string]$data.machine)) + "," + $nl)
[void]$sb.Append("  " + '"utc": ' + (Escape-JsonText ([string]$data.utc)) + "," + $nl)
[void]$sb.Append("  " + '"allowList": [' + $nl)
for ($i = 0; $i -lt $AllowList.Count; $i++) {
    $comma = ","
    if ($i -eq ($AllowList.Count - 1)) { $comma = "" }
    [void]$sb.Append("    " + (Escape-JsonText ([string]$AllowList[$i])) + $comma + $nl)
}
[void]$sb.Append("  ]," + $nl)
[void]$sb.Append("  " + '"categoryCount": ' + $rows.Count + "," + $nl)
[void]$sb.Append("  " + '"categories": [' + $nl)
for ($c = 0; $c -lt $rows.Count; $c++) {
    $row = $rows[$c]
    [void]$sb.Append("    {" + $nl)
    [void]$sb.Append("      " + '"category": ' + (Escape-JsonText ([string]$row.category)) + "," + $nl)
    [void]$sb.Append("      " + '"identifier": ' + (Escape-JsonText ([string]$row.identifier)) + "," + $nl)
    [void]$sb.Append("      " + '"type": ' + (Escape-JsonText ([string]$row.type)) + "," + $nl)
    [void]$sb.Append("      " + '"counters": [' + $nl)
    $counters = @($row.counters)
    for ($k = 0; $k -lt $counters.Count; $k++) {
        $ctr = $counters[$k]
        $ccomma = ","
        if ($k -eq ($counters.Count - 1)) { $ccomma = "" }
        [void]$sb.Append("        {" + $nl)
        [void]$sb.Append("          " + '"name": ' + (Escape-JsonText ([string]$ctr.name)) + "," + $nl)
        [void]$sb.Append("          " + '"identifier": ' + (Escape-JsonText ([string]$ctr.identifier)) + $nl)
        [void]$sb.Append("        }" + $ccomma + $nl)
    }
    [void]$sb.Append("      ]" + $nl)
    $rcomma = ","
    if ($c -eq ($rows.Count - 1)) { $rcomma = "" }
    [void]$sb.Append("    }" + $rcomma + $nl)
}
[void]$sb.Append("  ]" + $nl)
[void]$sb.Append("}" + $nl)

$dir = Split-Path -Parent $outPath
if ($dir -and -not (Test-Path $dir)) { New-Item -ItemType Directory -Path $dir | Out-Null }
$utf8 = New-Object System.Text.UTF8Encoding $false
[System.IO.File]::WriteAllText($outPath, $sb.ToString(), $utf8)
Write-Output $outPath
